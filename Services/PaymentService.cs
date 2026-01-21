using System.Text;
using System.Text.Json;

namespace PayRollManagementSystem.Services
{
    public interface IPaymentService
    {
        Task<SSLCommerzResponse> InitiatePayment(SSLCommerzRequest request);
        Task<SSLCommerzValidationResponse> ValidateTransaction(string valId);
        Task<bool> ProcessRefund(string bankTransactionId, decimal amount, string reason);
    }

    public class PaymentService : IPaymentService
    {
        private readonly SSLCommerzConfig _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(SSLCommerzConfig config, HttpClient httpClient, ILogger<PaymentService> logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<SSLCommerzResponse> InitiatePayment(SSLCommerzRequest request)
        {
            try
            {
                // Set store credentials
                request.store_id = _config.StoreId;
                request.store_passwd = _config.StorePassword;

                // Convert request to form data
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("store_id", request.store_id),
                    new KeyValuePair<string, string>("store_passwd", request.store_passwd),
                    new KeyValuePair<string, string>("total_amount", request.total_amount.ToString("F2")),
                    new KeyValuePair<string, string>("currency", request.currency),
                    new KeyValuePair<string, string>("tran_id", request.tran_id),
                    new KeyValuePair<string, string>("success_url", request.success_url),
                    new KeyValuePair<string, string>("fail_url", request.fail_url),
                    new KeyValuePair<string, string>("cancel_url", request.cancel_url),
                    new KeyValuePair<string, string>("ipn_url", request.ipn_url),
                    new KeyValuePair<string, string>("cus_name", request.cus_name),
                    new KeyValuePair<string, string>("cus_email", request.cus_email),
                    new KeyValuePair<string, string>("cus_add1", request.cus_add1),
                    new KeyValuePair<string, string>("cus_city", request.cus_city),
                    new KeyValuePair<string, string>("cus_postcode", request.cus_postcode),
                    new KeyValuePair<string, string>("cus_country", request.cus_country),
                    new KeyValuePair<string, string>("cus_phone", request.cus_phone),
                    new KeyValuePair<string, string>("product_name", request.product_name),
                    new KeyValuePair<string, string>("product_category", request.product_category),
                    new KeyValuePair<string, string>("product_profile", request.product_profile.ToString()),
                    new KeyValuePair<string, string>("shipping_method", request.shipping_method),
                    new KeyValuePair<string, string>("num_of_item", request.num_of_item.ToString()),
                    new KeyValuePair<string, string>("value_a", request.value_a),
                    new KeyValuePair<string, string>("value_b", request.value_b),
                    new KeyValuePair<string, string>("value_c", request.value_c),
                    new KeyValuePair<string, string>("value_d", request.value_d)
                });

                var response = await _httpClient.PostAsync(_config.ApiUrl, formContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"SSLCommerz Response: {responseContent}");

                var sslResponse = JsonSerializer.Deserialize<SSLCommerzResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                if (sslResponse == null)
                {
                    _logger.LogError("Failed to deserialize SSLCommerz response");
                    return new SSLCommerzResponse { status = "FAILED", failedreason = "Invalid response from payment gateway" };
                }

                return sslResponse;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError($"JSON Error initiating payment: {jsonEx.Message}");
                _logger.LogError($"JSON Error details: {jsonEx.StackTrace}");
                return new SSLCommerzResponse { status = "FAILED", failedreason = $"JSON Error: {jsonEx.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initiating payment: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return new SSLCommerzResponse { status = "FAILED", failedreason = ex.Message };
            }
        }

        public async Task<SSLCommerzValidationResponse> ValidateTransaction(string valId)
        {
            try
            {
                var validationUrl = $"{_config.ValidationUrl}?val_id={valId}&store_id={_config.StoreId}&store_passwd={_config.StorePassword}&format=json";

                var response = await _httpClient.GetAsync(validationUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"SSLCommerz Validation Response: {responseContent}");

                var validationResponse = JsonSerializer.Deserialize<SSLCommerzValidationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                return validationResponse ?? new SSLCommerzValidationResponse { status = "INVALID" };
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError($"JSON Error validating transaction: {jsonEx.Message}");
                return new SSLCommerzValidationResponse { status = "INVALID", error = jsonEx.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating transaction: {ex.Message}");
                return new SSLCommerzValidationResponse { status = "INVALID", error = ex.Message };
            }
        }

        public async Task<bool> ProcessRefund(string bankTransactionId, decimal amount, string reason)
        {
            try
            {
                // SSLCommerz refund API endpoint
                var refundUrl = _config.IsSandbox
                    ? "https://sandbox.sslcommerz.com/validator/api/merchantTransIDvalidationAPI.php"
                    : "https://securepay.sslcommerz.com/validator/api/merchantTransIDvalidationAPI.php";

                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("refund_amount", amount.ToString("F2")),
                    new KeyValuePair<string, string>("refund_remarks", reason),
                    new KeyValuePair<string, string>("bank_tran_id", bankTransactionId),
                    new KeyValuePair<string, string>("store_id", _config.StoreId),
                    new KeyValuePair<string, string>("store_passwd", _config.StorePassword)
                });

                var response = await _httpClient.PostAsync(refundUrl, formContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"SSLCommerz Refund Response: {responseContent}");

                // Parse response to check if refund was successful
                using var doc = JsonDocument.Parse(responseContent);
                var status = doc.RootElement.GetProperty("status").GetString();
                
                return status?.ToUpper() == "SUCCESS";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing refund: {ex.Message}");
                return false;
            }
        }
    }
}
