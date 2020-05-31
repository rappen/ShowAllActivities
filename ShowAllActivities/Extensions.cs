using Jonas;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace ShowAllActivities
{
    public static class Extensions
    {
        public static bool ReplaceRegardingCondition(this QueryExpression query, JonasPluginBag bag, bool makeDistinct)
        {
            bag.TraceBlockStart();
            if (query.EntityName != "activitypointer")
            {
                bag.Trace($"Wrong entity: {query.EntityName}");
                bag.TraceBlockEnd();
                return false;
            }
            var nullCondition = query.GetCondition("activityid", ConditionOperator.Null, bag.TracingService);
            if (nullCondition == null)
            {
                bag.Trace("No null condition for activityid");
                bag.TraceBlockEnd();
                return false;
            }
            Guid contactId;
            var regardingCondition = query.GetCondition("regardingobjectid", ConditionOperator.Equal, bag.TracingService);
            if (regardingCondition != null &&
                regardingCondition.Operator.Equals(ConditionOperator.Equal) &&
                regardingCondition.Values.Count == 1 &&
                regardingCondition.Values[0] is Guid regardingConditionId)
            {
                contactId = regardingConditionId;
                query.RemoveCondition(regardingCondition, bag.TracingService);
            }
            else
            {
                bag.Trace("No condition for regardingobjectid");
                var contactCondition = query.GetCondition("contactid", ConditionOperator.Equal, bag.TracingService);
                if (contactCondition != null &&
                    contactCondition.Operator.Equals(ConditionOperator.Equal) &&
                    contactCondition.Values.Count == 1 &&
                    contactCondition.Values[0] is Guid contactConditionId)
                {
                    contactId = contactConditionId;
                    query.RemoveCondition(contactCondition, bag.TracingService);
                }
                else
                {
                    bag.Trace("No condition for contactid");
                    bag.TraceBlockEnd();
                    return false;
                }
            }
            if (contactId.Equals(Guid.Empty))
            {
                bag.Trace("No contactid identified in query");
                bag.TraceBlockEnd();
                return false;
            }

            query.RemoveCondition(nullCondition, bag.TracingService);

            bag.Trace("Adding link-entity and condition for activity party");
            var leActivityparty = query.AddLink("activityparty", "activityid", "activityid");
            leActivityparty.LinkCriteria.AddCondition("partyid", ConditionOperator.Equal, contactId);

            if (makeDistinct)
            {
                query.Distinct = true;
            }
            bag.TraceBlockEnd();
            return true;
        }

        public static bool ReplaceRegardingCondition(this FetchExpression fetch, JonasPluginBag bag, bool makeDistinct)
        {
            bag.TraceBlockStart();
            var query = (bag.Service.Execute(new FetchXmlToQueryExpressionRequest { FetchXml = fetch.Query }) as FetchXmlToQueryExpressionResponse)?.Query;
            if (query.ReplaceRegardingCondition(bag, makeDistinct))
            {
                fetch.Query = (bag.Service.Execute(new QueryExpressionToFetchXmlRequest { Query = query }) as QueryExpressionToFetchXmlResponse)?.FetchXml;
                return true;
            }
            bag.TraceBlockEnd();
            return false;
        }
    }
}
