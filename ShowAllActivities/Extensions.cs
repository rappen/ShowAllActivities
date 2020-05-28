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
    public static class Extensions
    {
        public static bool ReplaceRegardingCondition(this QueryExpression query, JonasPluginBag bag)
        {
            bag.TraceBlockStart();
            if (query.EntityName != "activitypointer" || query.Criteria == null || query.Criteria.Conditions == null || query.Criteria.Conditions.Count < 2)
            {
                bag.Trace("Not expected query");
                bag.TraceBlockEnd();
                return false;
            }

            ConditionExpression nullCondition = null;
            ConditionExpression regardingCondition = null;

            bag.Trace("Checking criteria for expected conditions");
            foreach (ConditionExpression cond in query.Criteria.Conditions)
            {
                if (cond.AttributeName == "activityid" && cond.Operator == ConditionOperator.Null)
                {
                    bag.Trace("Found triggering null condition");
                    nullCondition = cond;
                }
                else if (cond.AttributeName == "regardingobjectid" && cond.Operator == ConditionOperator.Equal && cond.Values.Count == 1 && cond.Values[0] is Guid)
                {
                    bag.Trace("Found condition for regardingobjectid");
                    regardingCondition = cond;
                }
                else
                {
                    bag.Trace($"Disregarding condition for {cond.AttributeName}");
                }
            }
            if (nullCondition == null || regardingCondition == null)
            {
                bag.Trace("Missing expected null condition or regardingobjectid condition");
                bag.TraceBlockEnd();
                return false;
            }
            var regardingId = (Guid)regardingCondition.Values[0];
            bag.Trace($"Found regarding id: {regardingId}");

            bag.Trace("Removing triggering conditions");
            query.Criteria.Conditions.Remove(nullCondition);
            query.Criteria.Conditions.Remove(regardingCondition);

            bag.Trace("Adding link-entity and condition for activity party");
            var leActivityparty = query.AddLink("activityparty", "activityid", "activityid");
            leActivityparty.LinkCriteria.AddCondition("partyid", ConditionOperator.Equal, regardingId);
            bag.TraceBlockEnd();
            return true;
        }

        public static bool ReplaceRegardingCondition(this FetchExpression fetch, JonasPluginBag bag)
        {
            bag.TraceBlockStart();
            var query = (bag.Service.Execute(new FetchXmlToQueryExpressionRequest { FetchXml = fetch.Query }) as FetchXmlToQueryExpressionResponse)?.Query;
            if (query.ReplaceRegardingCondition(bag))
            {
                fetch.Query = (bag.Service.Execute(new QueryExpressionToFetchXmlRequest { Query = query }) as QueryExpressionToFetchXmlResponse)?.FetchXml;
                return true;
            }
            bag.TraceBlockEnd();
            return false;
        }

        private static bool ReplaceRegardingConditionUCI(QueryExpression query, ITracingService tracer)
        {
            if (query.EntityName != "activitypointer" || query.Criteria == null || query.Criteria.Conditions == null || query.Criteria.Filters[0].Conditions.Count < 2)
            {
                tracer.Trace("Not expected query");
                return false;
            }

            ConditionExpression nullCondition = null;
            ConditionExpression regardingCondition = null;

            tracer.Trace("Checking criteria for expected conditions");
            foreach (ConditionExpression cond in query.Criteria.Filters[0].Conditions)
            {
                if (cond.AttributeName == "activityid" && cond.Operator == ConditionOperator.Null)
                {
                    tracer.Trace("Found triggering null condition");
                    nullCondition = cond;
                }
                else if (cond.AttributeName == "regardingobjectid" && cond.Operator == ConditionOperator.Equal && cond.Values.Count == 1 && cond.Values[0] is Guid)
                {
                    tracer.Trace("Found condition for regardingobjectid");
                    regardingCondition = cond;
                }
                else
                {
                    tracer.Trace($"Disregarding condition for {cond.AttributeName}");
                }
            }

            foreach (ConditionExpression cond in query.LinkEntities[0].LinkCriteria.Conditions)
            {
                if (cond.AttributeName == "contactid" && cond.Operator == ConditionOperator.Equal && cond.Values.Count == 1)
                {
                    tracer.Trace("Found condition for regardingobjectid/contactid");
                    regardingCondition = cond;
                }
                else
                {
                    tracer.Trace($"Disregarding condition for {cond.AttributeName}");
                }
            }


            if (nullCondition == null || regardingCondition == null)
            {
                tracer.Trace("Missing expected null condition or regardingobjectid condition");
                return false;
            }
            var regardingId = (Guid)regardingCondition.Values[0];
            tracer.Trace($"Found regarding id: {regardingId}");

            tracer.Trace("Removing triggering conditions");
            query.Criteria.Filters[0].Conditions.Remove(nullCondition);
            query.Criteria.Filters[0].Conditions.Remove(regardingCondition);
            query.LinkEntities.Remove(query.LinkEntities[0]);

            tracer.Trace("Adding link-entity and condition for activity party");
            var leActivityparty = query.AddLink("activityparty", "activityid", "activityid");
            leActivityparty.LinkCriteria.AddCondition("partyid", ConditionOperator.Equal, regardingId);
            return true;
        }
    }
}
