using Mapster;
using System.Data;

namespace Giacom.Cdr.Domain
{
    public static class MapsterExtension
    {
        public static void MapModels(this TypeAdapterConfig config)
        {
            config.ForType<IDataReader, List<CallDetail>>().MapWith(reader => reader.ToCallDetails());
        }

        public static List<CallDetail> ToCallDetails(this IDataReader reader)
        {
            var result = new List<CallDetail>();

            int idxCaller = reader.GetOrdinal("caller_id");
            int idxRecipient = reader.GetOrdinal("recipient");
            int idxCallDate = reader.GetOrdinal("call_date");
            int idxEndTime = reader.GetOrdinal("end_time");
            int idxDuration = reader.GetOrdinal("duration");
            int idxCost = reader.GetOrdinal("cost");
            int idxReference = reader.GetOrdinal("reference");
            int idxCurrency = reader.GetOrdinal("currency");

            while (reader.Read())
            {
                var callDetail = new CallDetail(
                    Caller: reader.IsDBNull(idxCaller) ? null : reader.GetString(idxCaller),
                    Recipient: reader.IsDBNull(idxRecipient) ? null : reader.GetString(idxRecipient),
                    StartDateTime: reader.IsDBNull(idxCallDate) ? null : reader.GetDateTime(idxCallDate),
                    EndDateTime: reader.IsDBNull(idxEndTime) ? null : reader.GetDateTime(idxEndTime),
                    Duration: reader.IsDBNull(idxDuration) ? null : Convert.ToInt32(reader.GetInt64(idxDuration)),
                    Cost: reader.IsDBNull(idxCost) ? null : Convert.ToDecimal(reader.GetDouble(idxCost)),
                    Reference: reader.IsDBNull(idxReference) ? string.Empty : reader.GetString(idxReference),
                    Currency: reader.IsDBNull(idxCurrency) ? null : reader.GetString(idxCurrency)
                );
                result.Add(callDetail);
            }
            return result;
        }
    }


}
