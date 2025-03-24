using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Endpoints.Products.GetById
{
    public record GetProductsRequest(int Id);


    public class GetProductsRequestValidator : AbstractValidator<GetProductsRequest>
    {
        public GetProductsRequestValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greather than zero");
        }
    }

}