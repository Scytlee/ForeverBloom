module.exports = {
    request: {
        https: {
            // Default to true for proper certificate validation
            // For local development with self-signed certificates, you can temporarily set this to false
            // or configure your local environment to trust the development certificate
            rejectUnauthorized: true
        }
    },
    environments: {
        $default: {
            base: "https://localhost:7299/api/v1",
            adminKey: "AdminKey",
            frontendKey: "FrontendKey"
        }
    }
};
