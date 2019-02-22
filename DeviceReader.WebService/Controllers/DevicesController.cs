using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.WebService.Exeptions;
using DeviceReader.WebService.Filters;
using DeviceReader.WebService.Models.DeviceApiModels;
using Microsoft.AspNetCore.Authorization;
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

        [HttpGet("{id}/executables", Name = "GetAgentExecutables")]
        public async Task<DeviceApiAgentExecutableListModel> GetAgentExecutables([FromRoute] string  id)
        {
            if (!_deviceManager.GetDeviceListAsync().Result.Any(x => x.Id == id))
            {
                throw new ResourceNotFoundException();
            }

            var device = _deviceManager.GetDevice<IDevice>(id);
            //return DeviceApiModel.FromServiceModel(device);
            return DeviceApiAgentExecutableListModel.FromServiceModel(device);
        }

        [HttpGet("{id}/executables/{executable}", Name = "GetAgentExecutableDetails")]
        public async Task<DeviceApiAgentExecutableModel> GetAgentExecutableDetails([FromRoute] string id, [FromRoute] string executable)
        {
            if (!_deviceManager.GetDeviceListAsync().Result.Any(x => x.Id == id))
            {
                throw new ResourceNotFoundException();
            }

            var device = _deviceManager.GetDevice<IDevice>(id);
            //return DeviceApiModel.FromServiceModel(device);
            return DeviceApiAgentExecutableModel.FromServiceModel(device, executable);
        }


        // POST: api/devices/{id}/inbound
        // https://stackoverflow.com/questions/51328992/asp-net-core-server-side-validation-failure-causes-microsoft-aspnetcore-mvc-seri
        // https://weblog.west-wind.com/posts/2017/Sep/14/Accepting-Raw-Request-Body-Content-in-ASPNET-Core-API-Controllers
        // https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.1#automatic-http-400-responses
        // https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/custom-formatters?view=aspnetcore-2.1#read-write
        // now, perhaps we should accept multitude of input formats?
        // like me14, xml etc. Determined by Content-Type ? Later!
        // as sexy as it would be to receive already formatted observation
        // it requires to know device id to get formatinbound format for device
        // altho we could theoretically allow all formats to be posted and 
        // decide format based on content-type ? 
        // So it is probaly better to receive entire body of request here and 
        // try to parse it according to device inbound config


        [HttpPost("{id}/inbound")]
        [ProducesResponseType(204)]
        [Authorize()]
        public async Task Post([FromRoute] string id, [FromBody] string value)
        {         
            // move this to a separate middleware as existence of device should happen before authentication even.
            /*
            if (!_deviceManager.GetDeviceListAsync().Result.Any(x => x.Id == id))
            {
                throw new ResourceNotFoundException();
            }
            */
            var device = _deviceManager.GetDevice<IDevice>(id);
            
            /*
            if (!device.AcceptsInboundMessages)
            {
                throw new BadReqestException("Inbound messaging not enabled");
            }
            */
            // we should get message format here.

            _logger.Debug($"Inbound message: '{value}'", () => { });

            await device.InboundChannel.SendAsync(value);           
        }

        [HttpPost("{id}/restart")]
        [ProducesResponseType(204)]        
        public async Task Restart([FromRoute] string id, [FromBody] string value)
        {
            
            var device = _deviceManager.GetDevice<IDevice>(id);
            
            _logger.Debug($"Device {device.Id}: Restart requested from webservice", () => { });

            await device.StopAsync();
            await device.StartAsync();            
        }

        /*
        // POST: api/Devices
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }
        */

        /*
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
        */
    }
}
