using Domain.Errors;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class BaseController : ControllerBase
{
  protected ActionResult HandleFailure(Result result)
  {
    if (result.IsSuccess) throw new InvalidOperationException();

    return result.Error!.Code switch
    {
      ErrorCodes.BadRequest => BadRequest(result.Error),
      ErrorCodes.Unauthorized => Unauthorized(result.Error),
      ErrorCodes.TooManyRequests => StatusCode(429, result.Error),
      ErrorCodes.NotFound => NotFound(result.Error),
      ErrorCodes.Conflict => Conflict(result.Error),
      _ => StatusCode(StatusCodes.Status500InternalServerError, result.Error)
    };
  }
}
