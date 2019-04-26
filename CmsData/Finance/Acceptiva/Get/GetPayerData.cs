﻿using CmsData.Finance.Acceptiva.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CmsData.Finance.Acceptiva.Get
{
    internal class GetPayerData : AcceptivaRequest
    {
        private const string action = "get_payer_data";

        public GetPayerData(string apiKey, int peopleId)
            : base(apiKey, action)
        {
            Data["params[0][client_payer_id]"] = peopleId.ToString();
        }

        public new AcceptivaResponse<PayerDataResponse> Execute()
        {
            var response = base.Execute();
            var chargeResponse = JsonConvert.DeserializeObject<List<AcceptivaResponse<PayerDataResponse>>>(response);
            return chargeResponse[0];
        }
    }
}
