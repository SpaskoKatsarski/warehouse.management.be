﻿using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Common.MessageConstants.Keys;
using WarehouseManagement.Core.Contracts;
using WarehouseManagement.Core.DTOs;
using WarehouseManagement.Core.DTOs.Delivery;
using WarehouseManagement.Core.DTOs.Vendor;
using WarehouseManagement.Infrastructure.Data.Models;
using static WarehouseManagement.Common.MessageConstants.Keys.DeliveryMessageKeys;

namespace WarehouseManagement.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService deliveryService;
    private readonly IVendorService vendorService;
    private readonly IMarkerService markerService;

    public DeliveryController(
        IDeliveryService deliveryService,
        IVendorService vendorService,
        IMarkerService markerService
    )
    {
        this.deliveryService = deliveryService;
        this.vendorService = vendorService;
        this.markerService = markerService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(200, Type = typeof(DeliveryDto))]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDelivery(int id)
    {
        var model = await deliveryService.GetByIdAsync(id);

        return Ok(model);
    }

    [HttpGet("all")]
    [ProducesResponseType(200, Type = typeof(PageDto<DeliveryDto>))]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetDeliveries(
        [FromQuery] PaginationParameters paginationParams
    )
    {
        var model = await deliveryService.GetAllAsync(paginationParams);

        return Ok(model);
    }

    [HttpPost("add")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Add(DeliveryFormDto model)
    {
        if (!await vendorService.ExistByIdAsync(model.VendorId))
        {
            return BadRequest($"{VendorMessageKeys.VendorWithIdNotFound} {model.VendorId}");
        }

        var nonExistingMarkes = await markerService.GetNonExistingMarkerIdsAsync(model.Markers);

        if (nonExistingMarkes.Any())
        {
            return BadRequest(
                $"{MarkerMessageKeys.MarkerWithIdNotFound} {string.Join(",", nonExistingMarkes)}"
            );
        }

        var deliveryId = await deliveryService.AddAsync(model, User.Id());

        return Ok(deliveryId);
    }

    //TODO ONLY FOR ADMIN USERS
    [HttpPut("edit/{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Edit(int id, [FromBody] DeliveryFormDto model)
    {
        if (!await vendorService.ExistByIdAsync(model.VendorId))
        {
            return BadRequest($"{VendorMessageKeys.VendorWithIdNotFound} {model.VendorId}");
        }

        var nonExistingMarkes = await markerService.GetNonExistingMarkerIdsAsync(model.Markers);

        if (nonExistingMarkes.Any())
        {
            return BadRequest(
                $"{MarkerMessageKeys.MarkerWithIdNotFound} {string.Join(",", nonExistingMarkes)}"
            );
        }

        await deliveryService.EditAsync(id, model, User.Id());

        return Ok(DeliveryEditedSuccessfully);
    }

    //TODO ONLY FOR ADMIN USERS
    [HttpDelete("delete/{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        await deliveryService.DeleteAsync(id);

        return Ok(DeliveryDeletedSuccessfully);
    }

    [HttpPut("restore/{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Restore(int id)
    {
        await deliveryService.RestoreAsync(id);

        return Ok(DeliveryRestored);
    }

    [HttpGet("all-deleted")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<DeliveryDto>))]
    public async Task<IActionResult> AllDeleted()
    {
        var model = await deliveryService.GetAllDeletedAsync();

        return Ok(model);
    }

    [HttpGet("history/{id}")]
    [ProducesResponseType(200, Type = typeof(DeliveryHistoryDto))]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetHistory(int id)
    {
        var history = await deliveryService.GetHistoryAsync(id);

        return Ok(history);
    }

    [HttpPut("Approve/{id}")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<VendorDto>))]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Approve(int id)
    {
        await deliveryService.ApproveAsync(id);

        return Ok($"{DeliverySuccessfullyApproved} {id}");
    }
}
