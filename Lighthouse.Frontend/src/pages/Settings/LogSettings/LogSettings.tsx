import React, { useEffect, useState } from "react";
import { ApiServiceProvider } from "../../../services/Api/ApiServiceProvider";
import LighthouseLogViewer from "./LighthouseLogViewer";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { Button, Grid, Select, MenuItem, FormControl, InputLabel, SelectChangeEvent } from "@mui/material";
import DownloadIcon from '@mui/icons-material/Download';
import RefreshIcon from '@mui/icons-material/Refresh';

const LogSettings: React.FC = () => {
    const apiService = ApiServiceProvider.getApiService();

    const [logs, setLogs] = useState<string>('Loading...');
    const [logLevel, setLogLevel] = useState<string>('');
    const [supportedLogLevels, setSupportedLogLevels] = useState<string[]>([]);

    const refreshLogs = async () => {
        const currentLogs = await apiService.getLogs();
        setLogs(currentLogs);
    }

    const fetchLogLevel = async () => {
        const currentLogLevel = await apiService.getLogLevel();
        setLogLevel(currentLogLevel);
    }

    const fetchSupportedLogLevels = async () => {
        const currentSupportedLogLevels = await apiService.getSupportedLogLevels();
        setSupportedLogLevels(currentSupportedLogLevels);
    }

    const onDownload = () => {
        const today = new Date();
        const year = today.getFullYear();
        const month = String(today.getMonth() + 1).padStart(2, '0');
        const day = String(today.getDate()).padStart(2, '0');

        const formattedDate = `${year}${month}${day}`;

        const blob = new Blob([logs], { type: 'text/plain;charset=utf-8' });

        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `log-${formattedDate}.txt`;

        document.body.appendChild(link);

        link.click();

        document.body.removeChild(link);
    }


    const onLogLevelChanged = async (event: SelectChangeEvent) => {
        const newLogLevel = event.target.value as string;
        await apiService.setLogLevel(newLogLevel);
        setLogLevel(newLogLevel);
    }

    useEffect(() => {
        const initialize = async () => {
            await fetchSupportedLogLevels();
            await fetchLogLevel();
            await refreshLogs();
        };

        initialize();
    }, []); // eslint-disable-line react-hooks/exhaustive-deps

    return (
        <InputGroup title={'Logs'}>
            <Grid container spacing={2}>
                <Grid item xs={12}>
                    <FormControl fullWidth margin="normal">
                        <InputLabel>Log Level</InputLabel>
                        <Select
                            value={logLevel}
                            onChange={onLogLevelChanged}
                            label="Log Level"
                        >
                            {supportedLogLevels.map(level => (
                                <MenuItem key={level} value={level}>
                                    {level}
                                </MenuItem>
                            ))}
                        </Select>
                    </FormControl>
                </Grid>
                <Grid item xs={12}>
                    <Button onClick={onDownload} variant="outlined" startIcon={<DownloadIcon />}>
                        Download
                    </Button>
                    <Button onClick={refreshLogs} variant="outlined" startIcon={<RefreshIcon />}>
                        Refresh
                    </Button>
                </Grid>
                <Grid item xs={12}>
                    <LighthouseLogViewer data={logs} />
                </Grid>
            </Grid>
        </InputGroup>
    );
}

export default LogSettings;
