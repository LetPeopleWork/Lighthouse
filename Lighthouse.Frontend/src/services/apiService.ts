import axios from 'axios';
import { Team } from '../models/Team';
import { Project } from '../models/Project';
import { Forecast } from '../models/Forecast';

// Read Base URL from env?
const API_BASE_URL = '/api';

const apiService = axios.create({
    baseURL: API_BASE_URL,
});

export const getHeartbeat = async () => {
    try {
        const response = await apiService.get('/heartbeat');
        return response.data;
    } catch (error) {
        console.error('Error fetching heartbeat:', error);
    }
};

export function getProjectOverview(): Project[] {
    var binaryBlazer = new Team("Binary Blazers", 1)
    var mavericks = new Team("Mavericks", 2)
    var cyberSultans = new Team("Cyber Sultans", 3)
    var techEagles = new Team("Tech Eagles", 4)

    var lastUpdated = new Date("06/23/2024 12:41");

    var projects: Project[] = [
        new Project("Release 1.33.7", 1, 35, [binaryBlazer], [new Forecast(50, new Date("07/31/2024")), new Forecast(70, new Date("08/05/2024")), new Forecast(85, new Date("08/09/2024")), new Forecast(95, new Date("08/14/2024"))], lastUpdated),
        new Project("Release 42", 2, 28, [mavericks, cyberSultans], [new Forecast(50, new Date("07/09/2024")), new Forecast(70, new Date("07/11/2024")), new Forecast(85, new Date("07/14/2024")), new Forecast(95, new Date("07/17/2024"))], lastUpdated),
        new Project("Release Codename Daniel", 3, 33, [binaryBlazer, techEagles, mavericks, cyberSultans], [new Forecast(50, new Date("07/07/2024")), new Forecast(70, new Date("07/09/2024")), new Forecast(85, new Date("07/12/2024")), new Forecast(95, new Date("07/16/2024"))], lastUpdated)
    ]

    return projects
}