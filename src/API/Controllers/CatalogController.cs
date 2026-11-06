

using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace API.Controllers;

[ApiController]
[Route("api/catalogs")]
[Authorize(Policy = "RequireCatalogReadView")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogServices _catalogServices;

    public CatalogController(ICatalogServices catalogServices)
    {
        _catalogServices = catalogServices;
    }
    [HttpGet("sexCatalog")]
    public async Task<ActionResult<List<OptionDto>>> GetSexCatalog()
    {
        return await _catalogServices.GetAllSexOptions();
    }

    [HttpGet("bloodCatalog")]
    public async Task<ActionResult<List<OptionDto>>> GetBloodCatalog()
    {
        return await _catalogServices.GetAllbloodOptions();
    }


}
