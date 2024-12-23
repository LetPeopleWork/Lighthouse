const ADOTOKENNAME = 'AzureDevOpsLighthouseIntegrationTestToken';
const JIRATOKENNAME = 'JiraLighthouseIntegrationTestToken'
const LIGHTHOUSEURLNAME = 'LIGHTHOUSEURL';

const config = {
    baseUrl: 'http://localhost:8080/',
    adoToken: '',
    jiraToken: '',
};

const getEnvVariable = (name: string, defaultValue: string): string => {
    const value = process.env[name];
    
    if (!value) {
        console.log(`No value found for ${name} - using default`);
        return defaultValue;
    }
    
    return value;
};

config.baseUrl = getEnvVariable(LIGHTHOUSEURLNAME, config.baseUrl);
config.adoToken = getEnvVariable(ADOTOKENNAME, config.adoToken);
config.jiraToken = getEnvVariable(JIRATOKENNAME, config.jiraToken);

export default config;
