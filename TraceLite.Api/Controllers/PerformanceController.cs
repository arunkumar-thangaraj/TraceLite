using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceLite.Data;
using TraceLite.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace TraceLite.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PerformanceController : ControllerBase
    {
        private readonly TraceLiteDbContext _dbContext;

        public PerformanceController(TraceLiteDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<
            IEnumerable<PerformanceRecordResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            var records = await _dbContext.PerformanceRecords
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(100)
                .Select(x => new PerformanceRecordResponse
                {
                    Id = x.Id,
                    TraceId = x.TraceId,
                    Method = x.Method,
                    Path = x.Path,
                    StatusCode = x.StatusCode,
                    DurationMs = x.DurationMs,
                    CreatedAtUtc = x.CreatedAtUtc,

                    // The real endpoints behind e.g. POST /api/external/test
                    ExternalCallCount = _dbContext.HttpPerformanceRecords
                        .Count(http => http.TraceId == x.TraceId),

                    ExternalCalls = _dbContext.HttpPerformanceRecords
                        .Where(http => http.TraceId == x.TraceId)
                        .OrderBy(http => http.CreatedAtUtc)
                        .Take(3)
                        .Select(http => new ExternalCallSummary
                        {
                            Method = http.Method,
                            Url = http.Url,
                            StatusCode = http.StatusCode,
                            ErrorType = http.ErrorType
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            return Ok(records);
        }

        // Every stored outbound call, newest first, with the request that made it
        [HttpGet("http")]
        public async Task<ActionResult<
            IEnumerable<ExternalCallResponse>>> GetExternalCalls(
            CancellationToken cancellationToken)
        {
            var calls = await (
                    from http in _dbContext.HttpPerformanceRecords.AsNoTracking()
                    join request in _dbContext.PerformanceRecords.AsNoTracking()
                        on http.TraceId equals request.TraceId
                    orderby http.CreatedAtUtc descending
                    select new ExternalCallResponse
                    {
                        Id = http.Id,
                        TraceId = http.TraceId,
                        Method = http.Method,
                        Url = http.Url,
                        StatusCode = http.StatusCode,
                        DurationMs = http.DurationMs,
                        CreatedAtUtc = http.CreatedAtUtc,
                        ErrorType = http.ErrorType,
                        ErrorMessage = http.ErrorMessage,

                        RequestId = request.Id,
                        RequestMethod = request.Method,
                        RequestPath = request.Path,
                        RequestStatusCode = request.StatusCode,
                        RequestDurationMs = request.DurationMs,
                        RequestCreatedAtUtc = request.CreatedAtUtc
                    })
                .Take(200)
                .ToListAsync(cancellationToken);

            return Ok(calls);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<PerformanceRecordResponse>> GetById(
            long id,
            CancellationToken cancellationToken)
        {
            var record = await _dbContext.PerformanceRecords
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new PerformanceRecordResponse
                {
                    Id = x.Id,
                    TraceId = x.TraceId,
                    Method = x.Method,
                    Path = x.Path,
                    StatusCode = x.StatusCode,
                    DurationMs = x.DurationMs,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (record is null)
            {
                return NotFound(new
                {
                    message = $"Performance record {id} was not found."
                });
            }

            return Ok(record);
        }

        [HttpGet("sql")]
        public async Task<ActionResult<
    IEnumerable<SqlPerformanceRecordResponse>>> GetSqlRecords(
    CancellationToken cancellationToken)
        {
            var records = await _dbContext.SqlPerformanceRecords
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(100)
                .Select(x => new SqlPerformanceRecordResponse
                {
                    Id = x.Id,
                    TraceId = x.TraceId,
                    CommandType = x.CommandType,
                    CommandText = x.CommandText,
                    DurationMs = x.DurationMs,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ErrorType = x.ErrorType,
                    ErrorMessage = x.ErrorMessage
                })
                .ToListAsync(cancellationToken);

            return Ok(records);
        }


        [HttpGet("{id:long}/sql")]
        public async Task<IActionResult> GetSqlForRequest(
            long id,
            CancellationToken cancellationToken)
        {
            var performanceRecord =
                await _dbContext.PerformanceRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        record => record.Id == id,
                        cancellationToken);

            if (performanceRecord is null)
            {
                return NotFound();
            }

            var sqlRecords =
                await _dbContext.SqlPerformanceRecords
                    .AsNoTracking()
                    .Where(record =>
                        record.TraceId == performanceRecord.TraceId)
                    .OrderBy(record => record.CreatedAtUtc)
                    .Select(record => new SqlPerformanceRecordResponse
                    {
                        Id = record.Id,

                        TraceId = record.TraceId,

                        CommandType = record.CommandType,

                        CommandText = record.CommandText,

                        DurationMs = record.DurationMs,

                        CreatedAtUtc = record.CreatedAtUtc,

                        ErrorType = record.ErrorType,

                        ErrorMessage = record.ErrorMessage
                    })
                    .ToListAsync(cancellationToken);

            return Ok(sqlRecords);
        }

        [HttpGet("{id:long}/http")]
        public async Task<IActionResult> GetHttpForRequest(
            long id,
            CancellationToken cancellationToken)
        {
            var performanceRecord =
                await _dbContext.PerformanceRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        record => record.Id == id,
                        cancellationToken);

            if (performanceRecord is null)
            {
                return NotFound();
            }

            var httpRecords =
                await _dbContext.HttpPerformanceRecords
                    .AsNoTracking()
                    .Where(record =>
                        record.TraceId == performanceRecord.TraceId)
                    .OrderBy(record => record.CreatedAtUtc)
                    .Select(record =>
                        new HttpPerformanceRecordResponse
                        {
                            Id = record.Id,

                            TraceId = record.TraceId,

                            Method = record.Method,

                            Url = record.Url,

                            StatusCode = record.StatusCode,

                            DurationMs = record.DurationMs,

                            CreatedAtUtc = record.CreatedAtUtc,

                            ErrorType = record.ErrorType,

                            ErrorMessage = record.ErrorMessage
                        })
                    .ToListAsync(cancellationToken);

            return Ok(httpRecords);
        }
    }
}
