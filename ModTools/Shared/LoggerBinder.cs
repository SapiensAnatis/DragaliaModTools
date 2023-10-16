using System.CommandLine.Binding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace ModTools.Shared;


public class LoggerBinder : BinderBase<ILogger>
{
    protected override ILogger GetBoundValue(
        BindingContext bindingContext) => GetLogger(bindingContext);

    ILogger GetLogger(BindingContext bindingContext)
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(
            builder => builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.SingleLine = true;
                options.ColorBehavior = LoggerColorBehavior.Enabled;
            }));
        
        ILogger logger = loggerFactory.CreateLogger("ModTools");
        
        return logger;
    }
}
