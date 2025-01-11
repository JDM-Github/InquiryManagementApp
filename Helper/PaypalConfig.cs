using PayPalCheckoutSdk.Core;

namespace Helper {
    public static class PayPalConfig
    {
        public static PayPalEnvironment GetEnvironment()
        {
            var clientId = "Af0tB87keOdzXZpl_Ib8lb86Udu5oTWSL-xHDwAz4q9GiBQSFbejrkAqY2QQU5XAlYJ5PyFc6wsM45Wq";
            var clientSecret = "EO6ufyuol6bxnX_E9HV9OmpqgD9SCWI5AEEohSaLYjBpJqbsVzv650YBQDWk7mZgPIPqE0IRpoQ5Gcyu";

            var environment = new SandboxEnvironment(clientId, clientSecret); 
            return environment;
        }
    }
}
