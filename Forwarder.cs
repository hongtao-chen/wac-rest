namespace Rest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Web.Script.Serialization;
    using Microsoft.ManagementExperience.FeatureInterface;

    public class Forwarder : MarshalByRefObject, IFeature
    {
        private const string rest = "rest";
        private const string restHeaderPrefix = "x-wac-rest-";
        private const string schema = "schema";
        private const string skipCertificateCheck = "SkipCertificateCheck";
        private const string contentTypeHeader = "content-type";

        private static JavaScriptSerializer serializer = new JavaScriptSerializer();

        public string Name => rest;

        public FeatureTask Process(ProcessRequest request)
        {
            return new FeatureTask(ForwardRequest(request));
        }

        public static async Task<ProcessResponse> ForwardRequest(ProcessRequest request)
        {
            var response = new ProcessResponse();

            try
            {
                var restHeaders = FilterRestHeaders(request.Headers);
                await Call(
                    new HttpMethod(request.Method),
                    GetForwardUri(request, restHeaders),
                    restHeaders,
                    request.Body,
                    async r =>
                    {
                        response.Status = r.StatusCode;
                        foreach (var header in r.Headers.Concat(r.Content?.Headers))
                        {
                            var key = $"{restHeaderPrefix}{header.Key}";
                            if (!response.Headers.ContainsKey(key))
                            {
                                response.Headers[key] = string.Join(",", header.Value.ToArray());
                            }
                        }

                        response.Content = await r.Content?.ReadAsStringAsync();
                    });
            }
            catch (Exception e)
            {
                var message = e.InnerException?.Message ?? e.Message;
                response.Status = System.Net.HttpStatusCode.BadGateway;
                response.Content = serializer.Serialize(new { error = new { message } });
            }

            return response;
        }

        private static async Task Call(HttpMethod method, string address, IDictionary<string, string> headers, string content, Action<HttpResponseMessage> action)
        {
            using (var handler = new WebRequestHandler())
            {
                if (headers.ContainsKey(skipCertificateCheck) && string.Compare(headers[skipCertificateCheck], "true", true) == 0)
                {
                    handler.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                }

                using (var client = new HttpClient(handler))
                {
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }

                    using (var request = new HttpRequestMessage(method, address))
                    {
                        if (!string.IsNullOrEmpty(content))
                        {
                            using (var requestContent = new StringContent(content))
                            {
                                request.Content = requestContent;
                                request.Content.Headers.ContentType = new MediaTypeHeaderValue(
                                    headers.ContainsKey(contentTypeHeader) ? headers[contentTypeHeader] : "application/json");

                                using (var response = await client.SendAsync(request))
                                {
                                    action(response);
                                }
                            }
                        }
                        else
                        {
                            using (var response = await client.SendAsync(request))
                            {
                                action(response);
                            }
                        }
                    }
                }
            }
        }

        private static IDictionary<string, string> FilterRestHeaders(IDictionary<string, string> headers)
        {
            var result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var h in headers.Where(i => i.Key.StartsWith(restHeaderPrefix, StringComparison.InvariantCultureIgnoreCase)))
            {
                var rh = h.Key.ToLowerInvariant().Replace(restHeaderPrefix, string.Empty);
                if (!result.ContainsKey(rh))
                {
                    result[rh] = h.Value;
                }
            }

            return result;
        }

        private static string GetForwardUri(ProcessRequest request, IDictionary<string, string> headers)
        {
            var httpSchema = headers.ContainsKey(schema) ? headers[schema] : "https";

            var nodeIndex = Array.FindIndex(request.Uri.Segments, s => string.Compare(s, "nodes/", true) == 0);
            var host = request.Uri.Segments[nodeIndex + 1].Trim(new[] { '/' });

            var featureIndex = Array.FindIndex(request.Uri.Segments, s => string.Compare(s, rest + "/", true) == 0);
            var path = string.Join("", request.Uri.Segments.Skip(featureIndex + 1).ToArray());

            var forwardUri = $"{httpSchema}://{host}/{path}";
            if (!string.IsNullOrEmpty(request.Uri.Query))
            {
                forwardUri += request.Uri.Query;
            }

            return forwardUri;
        }
    }
}
