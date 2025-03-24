using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Endpoints.Products.GetById
{
    public class GetProductsRequest
    {
        [FromRoute]
        public int Id { get; set; }
    }


    public class GetProductsRequestValidator : AbstractValidator<GetProductsRequest>
    {
        public GetProductsRequestValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greather than zero");
        }
    }

}