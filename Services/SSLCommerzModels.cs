using System.Text.Json;

namespace PayRollManagementSystem.Services
{
    public class SSLCommerzConfig
    {
        public string StoreId { get; set; } = string.Empty;
        public string StorePassword { get; set; } = string.Empty;
        public bool IsSandbox { get; set; } = true;
        public string ApiUrl => IsSandbox 
            ? "https://sandbox.sslcommerz.com/gwprocess/v4/api.php" 
            : "https://securepay.sslcommerz.com/gwprocess/v4/api.php";
        public string ValidationUrl => IsSandbox
            ? "https://sandbox.sslcommerz.com/validator/api/validationserverAPI.php"
            : "https://securepay.sslcommerz.com/validator/api/validationserverAPI.php";
    }

    public class SSLCommerzRequest
    {
        public string store_id { get; set; } = string.Empty;
        public string store_passwd { get; set; } = string.Empty;
        public decimal total_amount { get; set; }
        public string currency { get; set; } = "BDT";
        public string tran_id { get; set; } = string.Empty;
        public string success_url { get; set; } = string.Empty;
        public string fail_url { get; set; } = string.Empty;
        public string cancel_url { get; set; } = string.Empty;
        public string ipn_url { get; set; } = string.Empty;
        
        // Customer Information
        public string cus_name { get; set; } = string.Empty;
        public string cus_email { get; set; } = string.Empty;
        public string cus_add1 { get; set; } = string.Empty;
        public string cus_city { get; set; } = string.Empty;
        public string cus_postcode { get; set; } = string.Empty;
        public string cus_country { get; set; } = "Bangladesh";
        public string cus_phone { get; set; } = string.Empty;
        
        // Product Information
        public string product_name { get; set; } = string.Empty;
        public string product_category { get; set; } = "Salary";
        public int product_profile { get; set; } = 1;
        
        // Shipping Information
        public string shipping_method { get; set; } = "NO";
        public int num_of_item { get; set; } = 1;
        
        // Optional
        public string value_a { get; set; } = string.Empty; // For storing PayrollId
        public string value_b { get; set; } = string.Empty; // For storing EmployeeId
        public string value_c { get; set; } = string.Empty;
        public string value_d { get; set; } = string.Empty;
    }

    public class SSLCommerzResponse
    {
        public string status { get; set; } = string.Empty;
        public string? failedreason { get; set; }
        public string? sessionkey { get; set; }
        public string? GatewayPageURL { get; set; }
        public string? storeBanner { get; set; }
        public string? storeLogo { get; set; }
        
        // Use JsonElement to handle different data types (string, array, object)
        public JsonElement? desc { get; set; }
        
        public string? is_direct_pay_enable { get; set; }
        
        // Helper property to safely get desc as string
        public string GetDescription()
        {
            if (desc == null || !desc.HasValue)
                return string.Empty;
            
            var element = desc.Value;
            
            // If it's a string, return it
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? string.Empty;
            
            // If it's an array or object, return the JSON representation
            return element.ToString();
        }
    }

    public class SSLCommerzValidationResponse
    {
        public string status { get; set; } = string.Empty;
        public string tran_date { get; set; } = string.Empty;
        public string tran_id { get; set; } = string.Empty;
        public string val_id { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public string store_amount { get; set; } = string.Empty;
        public string currency { get; set; } = string.Empty;
        public string bank_tran_id { get; set; } = string.Empty;
        public string card_type { get; set; } = string.Empty;
        public string card_brand { get; set; } = string.Empty;
        public string card_issuer { get; set; } = string.Empty;
        public string card_issuer_country { get; set; } = string.Empty;
        public string value_a { get; set; } = string.Empty;
        public string value_b { get; set; } = string.Empty;
        public string value_c { get; set; } = string.Empty;
        public string value_d { get; set; } = string.Empty;
        public string error { get; set; } = string.Empty;
    }
}
