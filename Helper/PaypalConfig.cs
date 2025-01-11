using PayPalCheckoutSdk.Core;

namespace Helper {
    public static class PayPalConfig
    {
        public static PayPalEnvironment GetEnvironment()
        {
            var clientId = "AX5AGNee2pN271sDzuXWPldkFw9x_97bvDEKdpXcwNgmdE26uTD64JjEi2XRpY2AWKyUn29kicp1mocQ";
            var clientSecret = "EIKjEnAUbgNvX6ZswF0WurD8ocna3IAiF7TyHnL_T8B_548iOSGgomNiQxFNOX3fl8CuE9uXX_ojyNup";

            var environment = new LiveEnvironment(clientId, clientSecret); 
            return environment;
        }
    }
}
