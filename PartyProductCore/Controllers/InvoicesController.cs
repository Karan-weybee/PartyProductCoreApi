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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PartyProductCore.Entities;
using PartyProductCore.Models;

namespace PartyProductCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private SqlConnection _connection;
        private string _connectionstring;
        private readonly PartyProductCoreContext _context;
        private readonly IMapper _mapper;
        private ILogger<InvoicesController> _logger;

        public InvoicesController(PartyProductCoreContext context, IMapper mapper, ILogger<InvoicesController> logger, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _connectionstring = configuration.GetConnectionString("defaultconnection");
            _connection = new SqlConnection(_connectionstring);
        }

        // GET: api/Invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices()
        {
            //var invoices = await _context.Invoice.FromSqlRaw("GetAllInvoices").ToListAsync();

            List<Invoice> data = new List<Invoice>();

            using (SqlCommand command = new SqlCommand("select i.id as Id,p.PartyName,pr.ProductName,i.Rate_Of_Product as RateOfInvoice,i.Quantity,i.DateOfInvoice,sum(i.Rate_Of_Product*i.Quantity) as total from invoices i inner join Parties p on i.Party_id=p.id inner join Products pr on i.Product_id = pr.id group by i.id, p.PartyName, pr.ProductName, i.DateOfInvoice, i.Rate_Of_Product, i.Quantity", _connection))
            {
                _connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new Invoice { Id = reader.GetInt32(0), PartyName = reader.GetString(1), ProductName = reader.GetString(2), RateOfProduct = reader.GetDecimal(3), Quantity = reader.GetInt32(4), DateOfInvoice = reader.GetDateTime(5), Total = reader.GetDecimal(6) });
                    }
                }
                _connection.Close();
            }

            return data;
        }


        [HttpGet("Search")]
        public async Task<ActionResult<List<Invoice>>> GetInvoices([FromQuery] int partyId, string productName, DateTime dateTime)
        {

            // return Ok(partyId + ":-" + productName + ":-" + dateTime);
            string formattedDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            List<Invoice> invoices = new List<Invoice>();
            formattedDateTime = formattedDateTime == "0001-01-01 00:00:00" ? "" : formattedDateTime;
            productName = productName == null ? "" : productName;

            List<Invoice> data = new List<Invoice>();

            using (SqlCommand command = new SqlCommand("  SELECT i.id,i.Rate_Of_Product as RateOfProduct,i.Quantity,i.DateOfInvoice,(i.Quantity*i.Rate_Of_Product) as Total ,p.PartyName,pr.ProductName FROM products pr INNER JOIN invoices i ON pr.id = i.Product_id INNER JOIN Parties p ON p.id = i.Party_id WHERE (pr.ProductName LIKE CONCAT('%', @productName, '%')) AND(p.id = @partyId) AND(  @DateOfInvoice = '' OR CONVERT(DATE, i.DateOfInvoice) = CONVERT(DATE, @DateOfInvoice) )", _connection))
            {
                command.Parameters.AddWithValue("@partyId", partyId);
                command.Parameters.AddWithValue("@productName", productName);
                command.Parameters.AddWithValue("@DateOfInvoice", formattedDateTime);
                _connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new Invoice { Id = reader.GetInt32(0), RateOfProduct = reader.GetDecimal(1), Quantity = reader.GetInt32(2), DateOfInvoice = reader.GetDateTime(3), Total = reader.GetDecimal(4), PartyName = reader.GetString(5), ProductName = reader.GetString(6) });
                    }
                }
                _connection.Close();
            }

            return data;

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
