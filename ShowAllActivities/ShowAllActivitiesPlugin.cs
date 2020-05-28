using Jonas;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAllActivities
{
    public class ShowAllActivitiesPlugin : JonasPluginBase
    {
        public override void Execute(JonasPluginBag bag)
        {
            var context = bag.PluginContext;
            var service = bag.Service;
            try
            {
                if (context.MessageName != "RetrieveMultiple" || context.Stage != 20 || context.Mode != 0 ||
                    !context.InputParameters.Contains("Query"))
                {
                    bag.Trace("Not expected context");
                    return;
                }

                if (context.InputParameters["Query"] is QueryExpression query)
                {
                    bag.trace("Checking QueryExpression");
#if DEBUG
                    var fetch1 = ((QueryExpressionToFetchXmlResponse)service.Execute(new QueryExpressionToFetchXmlRequest() { Query = query })).FetchXml;
                    bag.Trace($"Query before:\n{fetch1}");
#endif
                    if (query.ReplaceRegardingCondition(bag))
                    {
#if DEBUG
                        var fetch2 = ((QueryExpressionToFetchXmlResponse)service.Execute(new QueryExpressionToFetchXmlRequest() { Query = query })).FetchXml;
                        bag.Trace($"Query after:\n{fetch2}");
#endif
                        context.InputParameters["Query"] = query;
                    }
                }
                else if (context.InputParameters["Query"] is FetchExpression fetch)
                {
                    bag.trace("Checking FetchExpression");
                    bag.Trace($"Query before:\n{fetch.Query}");
                    if (fetch.ReplaceRegardingCondition(bag))
                    {
                        bag.Trace($"Query after:\n{fetch.Query}");
                        context.InputParameters["Query"] = fetch;
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}
