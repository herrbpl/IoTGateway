using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.WebService.Exeptions;
using DeviceReader.WebService.Filters;
using DeviceReader.WebService.Models.DeviceApiModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeviceReader.WebService.Controllers
{
    [Route("api/[controller]"), ExceptionsFilter]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        protected ILogger _logger;
        protected IDeviceManager _deviceManager;
        public DevicesController(ILogger logger, IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
            _logger = logger;
        }

        // GET: api/Devices
        [HttpGet]
        public async Task<DeviceApiListModel> Get()
        {
            //await _deviceManager.GetDeviceListAsync()
            //return new string[] { "value1", "value2" };
            var list = await _deviceManager.GetDeviceListAsync();
            return DeviceApiListModel.FromServiceModel(list);
        }

        // GET: api/Devices/5
        [HttpGet("{id}", Name = "Get")]
        public async Task<DeviceApiModel> Get(string id)
        {
            if (!_deviceManager.GetDeviceListAsync().Result.Any(x => x.Id == id)) {
                throw new ResourceNotFoundException();
            }
            
            var device = _deviceManager.GetDevice<IDevice>(id);
                //return DeviceApiModel.FromServiceModel(device);
            return DeviceApiModel.FromServiceModel(device);
            
        }

        // POST: api/Devices
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Devices/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
