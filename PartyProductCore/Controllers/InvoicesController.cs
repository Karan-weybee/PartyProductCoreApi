using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PartyProductCore.Entities;
using PartyProductCore.Models;

namespace PartyProductCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly PartyProductCoreContext _context;
        private readonly IMapper _mapper;
        private ILogger<InvoicesController> _logger;

        public InvoicesController(PartyProductCoreContext context, IMapper mapper, ILogger<InvoicesController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices()
        {
            var invoices = await _context.Invoices.FromSqlRaw("GetAllInvoices").ToListAsync();
            List<Invoice> invoice = new List<Invoice>();
            foreach (var item in invoices)
            {
                invoice.Add(new Invoice()
                {
                    Id = item.Id,
                    Quantity = item.Quantity,
                    RateOfProduct = item.RateOfProduct,
                    PartyName = _context.Parties.Find(item.PartyId).PartyName.ToString(),
                    ProductName = _context.Products.Find(item.ProductId).ProductName.ToString(),
                    DateOfInvoice = item.DateOfInvoice,
                    Total = item.Quantity * item.RateOfProduct
                });
            }

            return invoice;
        }

        // GET: api/Invoices/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<Invoice>> GetInvoices(int id)
        //{

        //    if (!InvoicesExists(id))
        //    {
        //        return NotFound("Invoice Not Found");
        //    }
        //    var invoices = await _context.Invoices.FromSqlRaw($"GetInvoice {id}").ToListAsync();

        //    Invoice invoice = new Invoice()
        //    {
        //        Id = id,
        //        RateOfProduct = invoices.First().RateOfProduct,
        //        Quantity = invoices.First().Quantity,
        //        PartyName = _context.Parties.Find(invoices.First().PartyId).PartyName.ToString(),
        //        ProductName = _context.Products.Find(invoices.First().ProductId).ProductName.ToString(),
        //        DateOfInvoice = invoices.First().DateOfInvoice,
        //        Total = invoices.First().Quantity * invoices.First().RateOfProduct
        //    };
        //    return invoice;
        //}


        [HttpGet("Search")]
        public async Task<ActionResult<List<Invoice>>> GetInvoices([FromQuery] int partyId, string productName, DateTime dateTime)
        {

            List<Invoices> invoices = new List<Invoices>();

            if (productName != null && dateTime.ToString() != "1/1/0001 12:00:00 AM")
            {
                string formattedDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss");

                invoices = await _context.Invoices.FromSqlRaw(
            "EXEC GetInvoiceFromProductNameAndDate @partyId, @productName, @DateOfInvoice",
            new SqlParameter("@partyId", partyId),
            new SqlParameter("@productName", productName),
            new SqlParameter("@DateOfInvoice", SqlDbType.DateTime) { Value = formattedDateTime }
        ).ToListAsync();
            }

            else if (productName != null && dateTime.ToString() == "1/1/0001 12:00:00 AM")
            {

                invoices = await _context.Invoices.FromSqlRaw(
           "EXEC GetInvoiceFromProductName @partyId, @productName",
           new SqlParameter("@partyId", partyId),
           new SqlParameter("@productName", productName)).ToListAsync();
            }

            else if (productName == null && dateTime.ToString() != "1/1/0001 12:00:00 AM")
            {
                string formattedDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss");

                invoices = await _context.Invoices.FromSqlRaw(
           "EXEC GetInvoiceFromDate @partyId, @DateOfInvoice",
           new SqlParameter("@partyId", partyId),
           new SqlParameter("@DateOfInvoice", SqlDbType.DateTime) { Value = formattedDateTime }).ToListAsync();
            }

            else if (productName == null && dateTime.ToString() == "1/1/0001 12:00:00 AM")
            {

                invoices = await _context.Invoices.FromSqlRaw(
           "EXEC GetInvoiceFromPartyId @partyId",
           new SqlParameter("@partyId", partyId)
      ).ToListAsync();
            }

            List<Invoice> invoice = new List<Invoice>();
            foreach (var item in invoices)
            {
                invoice.Add(new Invoice()
                {
                    Id = item.Id,
                    Quantity = item.Quantity,
                    RateOfProduct = item.RateOfProduct,
                    PartyName = _context.Parties.Find(item.PartyId).PartyName.ToString(),
                    ProductName = _context.Products.Find(item.ProductId).ProductName.ToString(),
                    DateOfInvoice = item.DateOfInvoice,
                    Total = item.Quantity * item.RateOfProduct
                });
            }

            return Ok(invoice);

        }

        // POST: api/Invoices
        [HttpPost]
        public async Task<ActionResult<InvoiceDTO>> PostInvoices(InvoiceDTO invoicesDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                await _context.Database.ExecuteSqlRawAsync("EXEC InsertInvoice @Rate_Of_Product, @Quantity, @Party_id, @Product_id,@Date",
                    new SqlParameter("@Rate_Of_Product", invoicesDTO.RateOfProduct),
                    new SqlParameter("@Quantity", invoicesDTO.Quantity),
                    new SqlParameter("@Party_id", invoicesDTO.PartyId),
                    new SqlParameter("@Product_id", invoicesDTO.ProductId),
                    new SqlParameter("@Date", DateTime.Today.Date));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return StatusCode(201, $"invoice Created Successfully");
        }

        // DELETE: api/Invoices
        [HttpDelete]
        public async Task<ActionResult<Invoices>> DeleteInvoices()
        {
            await _context.Database.ExecuteSqlRawAsync($"DeleteInvoice");

            return StatusCode(202, $"invoice Deleted Successfully");
        }



        private bool InvoicesExists(int id)
        {
            return _context.Invoices.Any(e => e.Id == id);
        }
        private bool ProductNameExists(string productName)
        {
            return _context.Products.Any(e => e.ProductName.Contains(productName));
        }
    }
}
