using Jonas;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace ShowAllActivities
{
    public class ShowAllActivitiesPlugin : JonasPluginBase
    {
        public override void Execute(JonasPluginBag bag)
        {
            try
            {
                if (bag.PluginContext.MessageName != "RetrieveMultiple" || bag.PluginContext.Stage != 20 || bag.PluginContext.Mode != 0 ||
                    !bag.PluginContext.InputParameters.Contains("Query"))
                {
                    bag.Trace("Not expected context");
                    return;
                }

                if (bag.PluginContext.InputParameters["Query"] is QueryExpression query)
                {
                    bag.trace("Checking QueryExpression");
#if DEBUG
                    var fetch1 = ((QueryExpressionToFetchXmlResponse)bag.Service.Execute(new QueryExpressionToFetchXmlRequest() { Query = query })).FetchXml;
                    bag.Trace($"Query before:\n{fetch1}");
#endif
                    if (query.ReplaceRegardingCondition(bag))
                    {
#if DEBUG
                        var fetch2 = ((QueryExpressionToFetchXmlResponse)bag.Service.Execute(new QueryExpressionToFetchXmlRequest() { Query = query })).FetchXml;
                        bag.Trace($"Query after:\n{fetch2}");
#endif
                        bag.PluginContext.InputParameters["Query"] = query;
                    }
                }
                else if (bag.PluginContext.InputParameters["Query"] is FetchExpression fetch)
                {
                    bag.trace("Checking FetchExpression");
                    bag.Trace($"Query before:\n{fetch.Query}");
                    if (fetch.ReplaceRegardingCondition(bag))
                    {
                        bag.Trace($"Query after:\n{fetch.Query}");
                        bag.PluginContext.InputParameters["Query"] = fetch;
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
