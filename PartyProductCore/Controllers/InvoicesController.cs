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


        [HttpGet("Search")]
        public async Task<ActionResult<List<Invoice>>> GetInvoices([FromQuery] int partyId, string productName, DateTime dateTime)
        {

            // return Ok(partyId + ":-" + productName + ":-" + dateTime);
            string formattedDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss"); ;
            List<Invoice> invoices = new List<Invoice>();
            formattedDateTime = formattedDateTime == "0001-01-01 00:00:00" ? "" : formattedDateTime;
            productName = productName == null ? "" : productName;


            invoices = await _context.Invoice.FromSqlRaw(
        "EXEC GetInvoiceFromProductNameAndDate @partyId, @productName, @DateOfInvoice",
        new SqlParameter("@partyId", partyId),
        new SqlParameter("@productName", productName),
        new SqlParameter("@DateOfInvoice", formattedDateTime)
    ).ToListAsync();

            //List<Invoice> invoice = new List<Invoice>();
            //foreach (var item in invoices)
            //{
            //    invoice.Add(new Invoice()
            //    {
            //        Id = item.Id,
            //        Quantity = item.Quantity,
            //        RateOfProduct = item.RateOfProduct,
            //        PartyName = _context.Parties.Find(item.PartyId).PartyName.ToString(),
            //        ProductName = _context.Products.Find(item.ProductId).ProductName.ToString(),
            //        DateOfInvoice = item.DateOfInvoice,
            //        Total = item.Quantity * item.RateOfProduct
            //    });
            //}

            return Ok(invoices);

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
