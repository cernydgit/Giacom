using System.Data;
using Mapster;

namespace Giacom.Cdr.Domain
{
    public static class MapsterExtension
    {
        /// <summary>
        /// Configures the mapping between IDataReader and a list of CallDetail objects.
        /// This allows automatic conversion using Mapster.
        /// </summary>
        /// <param name="config">The Mapster configuration to update.</param>
        public static void MapModels(this TypeAdapterConfig config)
        {
            config.ForType<IDataReader, List<CallDetail>>()
                  .MapWith(reader => reader.ToCallDetails());
        }

        /// <summary>
        /// Converts a data reader into a list of CallDetail objects.
        /// It reads each record in the data reader and maps columns to the CallDetail properties.
        /// </summary>
        /// <param name="reader">The IDataReader instance to convert.</param>
        /// <returns>A list of CallDetail objects.</returns>
        public static List<CallDetail> ToCallDetails(this IDataReader reader)
        {
            // Initialize the resulting list of CallDetail objects.
            var result = new List<CallDetail>();

            // Retrieve the column ordinal positions for each required field.
            int idxCaller = reader.GetOrdinal("caller_id");
            int idxRecipient = reader.GetOrdinal("recipient");
            int idxCallDate = reader.GetOrdinal("call_date");
            int idxEndTime = reader.GetOrdinal("end_time");
            int idxDuration = reader.GetOrdinal("duration");
            int idxCost = reader.GetOrdinal("cost");
            int idxReference = reader.GetOrdinal("reference");
            int idxCurrency = reader.GetOrdinal("currency");

            // Loop through each record in the data reader.
            while (reader.Read())
            {
                // Create a new CallDetail object mapping each field,
                // checking for DBNull values and converting as needed.
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
                // Add the mapped CallDetail object to the result list.
                result.Add(callDetail);
            }

            return result;
        }
    }


}
