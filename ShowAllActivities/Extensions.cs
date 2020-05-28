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
        public static bool ReplaceRegardingCondition(this QueryExpression query, JonasPluginBag bag)
        {
            bag.TraceBlockStart();
            if (query.EntityName != "activitypointer")
            {
                bag.Trace($"Wrong entity: {query.EntityName}");
                bag.TraceBlockEnd();
                return false;
            }
            var nullCondition = query.GetCondition("activityid", ConditionOperator.Null);
            if (nullCondition == null)
            {
                bag.Trace("No null condition for activityid");
                bag.TraceBlockEnd();
                return false;
            }
            var regardingCondition = query.GetCondition("regardingobjectid", ConditionOperator.Equal);
            if (regardingCondition == null || regardingCondition.Values.Count != 1 || !(regardingCondition.Values[0] is Guid))
            {
                bag.Trace("No condition for regardingobjectid");
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

        private static ConditionExpression GetCondition(this QueryExpression query, string attribute, ConditionOperator? oper)
        {
            var result = query.Criteria?.GetCondition(attribute, oper);
            if (result!=null)
            {
                return result;
            }
            foreach (var link in query.LinkEntities)
            {
                var linkresult = link.GetCondition(attribute, oper);
                if (linkresult != null)
                {
                    return linkresult;
                }
            }
            return null;
        }

        private static ConditionExpression GetCondition(this LinkEntity linkentity, string attribute, ConditionOperator? oper)
        {
            var result = linkentity.LinkCriteria?.GetCondition(attribute, oper);
            if (result != null)
            {
                return result;
            }
            foreach (var link in linkentity.LinkEntities)
            {
                var linkresult = link.GetCondition(attribute, oper);
                if (linkresult != null)
                {
                    return linkresult;
                }
            }
            return null;
        }

        private static ConditionExpression GetCondition(this FilterExpression filter, string attribute, ConditionOperator? oper)
        {
            var result = filter?.Conditions?.FirstOrDefault(c => c.AttributeName.Equals(attribute) && (oper == null || oper == c.Operator));
            if (result != null)
            {
                return result;
            }
            foreach (var subfilter in filter?.Filters)
            {
                if (subfilter.GetCondition(attribute, oper) is ConditionExpression subresult)
                {
                    return subresult;
                }
            }
            return null;
        }
    }
}
