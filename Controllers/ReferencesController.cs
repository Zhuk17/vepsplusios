using Microsoft.AspNetCore.Mvc;
using VepsPlusApi.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ReferencesController : ControllerBase
    {
        private readonly ILogger<ReferencesController> _logger;

        public ReferencesController(ILogger<ReferencesController> logger)
        {
            _logger = logger;
        }

        [HttpGet("fueltypes")]
        public IActionResult GetFuelTypes()
        {
            _logger.LogInformation("Getting fuel types.");
            var fuelTypes = new List<string> { "Бензин АИ-92", "Бензин АИ-95", "Бензин АИ-98", "Дизель", "Газ" };
            return Ok(new ApiResponse<List<string>> { IsSuccess = true, Data = fuelTypes, Message = "Список типов топлива успешно получен." });
        }

        [HttpGet("carmodels")]
        public IActionResult GetCarModels()
        {
            _logger.LogInformation("Getting car models.");
            var carModels = new List<string> { "Легковой", "Грузовой", "Мотоцикл", "Автобус" };
            return Ok(new ApiResponse<List<string>> { IsSuccess = true, Data = carModels, Message = "Список моделей автомобилей успешно получен." });
        }

        [HttpGet("projects")]
        public IActionResult GetProjects()
        {
            _logger.LogInformation("Getting projects.");
            var projects = new List<string> { "9401", "9005", "9002", "9403", "9404", "9402", "9405", "1887", "2119", "9003", "2129" };
            return Ok(new ApiResponse<List<string>> { IsSuccess = true, Data = projects, Message = "Список проектов успешно получен." });
        }

        [HttpGet("workers")]
        public IActionResult GetWorkers()
        {
            _logger.LogInformation("Getting workers.");
            var workers = new List<string> { "Все работники", "Работник А.А.", "Работник Б.Б.", "Работник В.В." };
            return Ok(new ApiResponse<List<string>> { IsSuccess = true, Data = workers, Message = "Список работников успешно получен." });
        }

        [HttpGet("statuses")]
        public IActionResult GetStatuses()
        {
            _logger.LogInformation("Getting statuses.");
            var statuses = new List<string> { "На рассмотрении", "Одобрено", "Отклонено" };
            return Ok(new ApiResponse<List<string>> { IsSuccess = true, Data = statuses, Message = "Список статусов успешно получен." });
        }
    }
}
