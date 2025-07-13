using FluentValidation;
using KiteConnectApi.Models.Trading;

namespace KiteConnectApi.Validators
{
    public class NiftyOptionStrategyConfigValidator : AbstractValidator<NiftyOptionStrategyConfig>
    {
        public NiftyOptionStrategyConfigValidator()
        {
            RuleFor(x => x.StrategyName).NotEmpty().WithMessage("Strategy Name is required.");
            RuleFor(x => x.UnderlyingInstrument).NotEmpty().WithMessage("Underlying Instrument is required.");
            RuleFor(x => x.Exchange).NotEmpty().WithMessage("Exchange is required.");
            RuleFor(x => x.ProductType).NotEmpty().WithMessage("Product Type is required.");
            RuleFor(x => x.FromDate).LessThanOrEqualTo(x => x.ToDate).WithMessage("From Date must be before or equal to To Date.");
            RuleFor(x => x.EntryTime).InclusiveBetween(0, 2359).WithMessage("Entry Time must be a valid time (HHMM).");
            RuleFor(x => x.ExitTime).InclusiveBetween(0, 2359).WithMessage("Exit Time must be a valid time (HHMM).");
            RuleFor(x => x.StopLossPercentage).GreaterThanOrEqualTo(0).WithMessage("Stop Loss Percentage must be non-negative.");
            RuleFor(x => x.TargetPercentage).GreaterThanOrEqualTo(0).WithMessage("Target Percentage must be non-negative.");
            RuleFor(x => x.TakeProfitPercentage).GreaterThanOrEqualTo(0).WithMessage("Take Profit Percentage must be non-negative.");
            RuleFor(x => x.MaxTradesPerDay).GreaterThanOrEqualTo(0).WithMessage("Max Trades Per Day must be non-negative.");
            RuleFor(x => x.HedgeDistancePoints).GreaterThanOrEqualTo(0).When(x => x.HedgePremiumPercentage == 0).WithMessage("Hedge Distance Points must be non-negative if Hedge Premium Percentage is not set.");
            RuleFor(x => x.HedgePremiumPercentage).GreaterThanOrEqualTo(0).When(x => x.HedgeDistancePoints == 0).WithMessage("Hedge Premium Percentage must be non-negative if Hedge Distance Points is not set.");
            RuleFor(x => x.OrderType).NotEmpty().WithMessage("Order Type is required.");
            RuleFor(x => x.ExecutionMode).NotEmpty().WithMessage("Execution Mode is required (Auto or Manual).").Must(BeValidExecutionMode).WithMessage("Execution Mode must be Auto or Manual.");
        }

        private bool BeValidExecutionMode(string mode)
        {
            return mode == "Auto" || mode == "Manual";
        }
    }
}