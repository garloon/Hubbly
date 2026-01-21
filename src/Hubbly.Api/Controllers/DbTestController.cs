using Hubbly.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hubbly.Api.Controllers;

[ApiController]
[Route("api/db-test")]
public class DbTestController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DbTestController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
                return BadRequest("Cannot connect to database");

            var tables = new List<string>();
            var connection = _context.Database.GetDbConnection();

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    ORDER BY table_name";

                await connection.OpenAsync();
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }
            }

            return Ok(new
            {
                Database = connection.Database,
                Server = connection.DataSource,
                Tables = tables,
                TableCount = tables.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}