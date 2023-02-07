using FluentValidation;
using Microsoft.Extensions.Localization;

namespace ValidationExample.Entities
{
    public class User
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public int? Age { get; set; }
        public List<Order>? Orders { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
    }

    public class Address
    {
        public List<string> Lines { get; set; }
        public string Town { get; set; }
        public string District { get; set; }
        public string City { get; set; }
    }

    public class UserValidator : AbstractValidator<User>
    {
        public UserValidator()
        {
            RuleFor(user => user.Name).NotEmpty().WithMessage("Name can not be empty!");
            RuleFor(user => user.Email).NotEmpty();
            RuleFor(user => user.Password).NotEmpty();
            //combining validator, age must be greater than 5,less than 100
            RuleFor(user => user.Age).LessThan(100).GreaterThan(5);

           

            //1st approach validation of list of complex type  
            RuleForEach(user => user.Orders).SetValidator(new OrderValidator());
            //2nd approach validation of list of complex type 
            RuleForEach(user => user.Orders).ChildRules(orders =>
            {
                orders.RuleFor(x => x.Id).GreaterThan(0);
            });
            //Business rule= users can not have more than 10 orders and all orders must have an id greater than 0
            RuleFor(user => user.Orders).NotNull().Must(orders => orders.Count <= 10).WithMessage("Only 10 orders are allowed");
            RuleForEach(user => user.Orders).NotNull().Must(orders => orders.Id > 0).WithMessage("Order Id must be greater than 0");
            //lets combine two rules.It is same with two rules above. It is recommended to use above due to code readability
            RuleFor(user => user.Orders).NotNull().Must(orders => orders.Count <= 10).WithMessage("Only 10 orders are allowed")
                .ForEach(order =>
                {
                    order.Must(order => order.Id > 0).WithMessage("Orders must have a total of more than 0");
                });

            //If you want to change the name of property you can use withName.Validation result will now be seen as "Full name must not be empty"
            RuleFor(user => user.Name).NotNull().WithName("Full name");
            //we can make our own custom rules using IRuleBuilder interface.
            RuleFor(user => user.Orders).ListMustContainFewerThan(10);
            //using include keyword for user name
            Include(new NameValidator());

            //using Ruleset for Name attribute,this ruleset will be called where validator class created and used.
            RuleSet("NameRuleset", () =>
            {
                RuleFor(user => user.Name).NotEmpty();
            });

            //Localization
            //to force all messages to be in turkish
            //ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("tr");
            //this rule will show the result that we added in CustomLanguageManager
            //RuleFor(user=>user.Age).NotEmpty().WithMessage(user=>
            //ValidatorOptions.Global.LanguageManager.GetString("test", new System.Globalization.CultureInfo("en")));
            //RuleFor(user => user.Name).NotEmpty().WithMessage(user =>
            //ValidatorOptions.Global.LanguageManager.GetString("test", new System.Globalization.CultureInfo("tr")));
        }
    }
    public class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator()
        {
            //validating list with RuleForEach
            RuleForEach(x => x.Lines).NotNull();
            //validating list with WithMessage when an element is null then {CollectionIndex} attribute will show us which element is null
            RuleForEach(x => x.Lines).NotNull().WithMessage("Address {CollectionIndex} is required.");
            RuleFor(address => address.Town).NotEmpty();
        }
    }
    public class OrderValidator : AbstractValidator<Order>
    {
        public OrderValidator()
        {
            //must have an id and must be greater than 0
            RuleFor(order => order.Id).NotEmpty().GreaterThan(0);
            RuleFor(order => order.Name).NotNull();
            //1st approach complex type validation ,we can use addressvalidator to validate address object in user
            RuleFor(order => order.Address).SetValidator(new AddressValidator());
            //2nd approach complex type validation ,we can validate one by one in user validator class
            RuleFor(order => order.Address.Town).NotEmpty();
            //we should check null for address in this case with condition 'when'
            RuleFor(order => order.Address.Town).NotEmpty().When(user => user.Address != null);
        }
    }
    //custom validator for lists
    public static class MyCustomListValidator
    {
        public static IRuleBuilderOptions<T, IList<TElement>> ListMustContainFewerThan<T, TElement>(this IRuleBuilder<T, IList<TElement>> ruleBuilder, int num)
        {
            return ruleBuilder.Must(list => list.Count < num).WithMessage("The list contains too many items");
        }
    }
    //validator for name,this validator can be used for other classes with 'include' keyword
    public class NameValidator : AbstractValidator<User>
    {
        public NameValidator()
        {
            RuleFor(x => x.Name).NotNull().Length(0, 255);
        }
    }
}
