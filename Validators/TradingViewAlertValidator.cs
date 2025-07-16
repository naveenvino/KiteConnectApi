using FluentValidation;
using KiteConnectApi.Models.Dto;

namespace KiteConnectApi.Validators
{
    public class TradingViewAlertValidator : AbstractValidator<TradingViewAlert>
    {
        public TradingViewAlertValidator()
        {
            RuleFor(x => x.StrategyName).NotEmpty().WithMessage("StrategyName is required.");
            RuleFor(x => x.Strike).GreaterThan(0).WithMessage("Strike must be greater than 0.");
            RuleFor(x => x.Type).NotEmpty().WithMessage("Type is required (CE or PE).").Must(BeValidOptionType).WithMessage("Type must be CE or PE.");
            RuleFor(x => x.Signal).NotEmpty().WithMessage("Signal is required.");
            RuleFor(x => x.Action).NotEmpty().WithMessage("Action is required (Entry or Stoploss).").Must(BeValidAction).WithMessage("Action must be Entry or Stoploss.");
        }

        private bool BeValidOptionType(string? type)
        {
            return type == "CE" || type == "PE";
        }

        private bool BeValidAction(string? action)
        {
            return action == "Entry" || action == "Stoploss";
        }
    }
}