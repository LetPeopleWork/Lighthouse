import React, { useContext, useEffect, useState } from "react";
import LighthouseLogViewer from "./LighthouseLogViewer";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { Button, Select, MenuItem, FormControl, InputLabel, SelectChangeEvent } from "@mui/material";
import Grid from '@mui/material/Grid2'
import DownloadIcon from '@mui/icons-material/Download';
import RefreshIcon from '@mui/icons-material/Refresh';
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const LogSettings: React.FC = () => {
    const [logs, setLogs] = useState<string>('Loading...');
    const [logLevel, setLogLevel] = useState<string>('');
    const [supportedLogLevels, setSupportedLogLevels] = useState<string[]>([]);

    const { logService } = useContext(ApiServiceContext);

    const refreshLogs = async () => {
        const currentLogs = await logService.getLogs();
        setLogs(currentLogs);
    }

    const fetchLogLevel = async () => {
        const currentLogLevel = await logService.getLogLevel();
        setLogLevel(currentLogLevel);
    }

    const fetchSupportedLogLevels = async () => {
        const currentSupportedLogLevels = await logService.getSupportedLogLevels();
        setSupportedLogLevels(currentSupportedLogLevels);
    }

    const onDownload = async () => {
        await logService.downloadLogs();
    }

    const onLogLevelChanged = async (event: SelectChangeEvent) => {
        const newLogLevel = event.target.value;
        await logService.setLogLevel(newLogLevel);
        setLogLevel(newLogLevel);
    }

    useEffect(() => {
        const initialize = async () => {
            await fetchSupportedLogLevels();
            await fetchLogLevel();
            await refreshLogs();
        };

        initialize();
    }, []);

    return (
        <InputGroup title={'Logs'}>
            <Grid container spacing={2}>
                <Grid  size={{ xs: 12 }}>
                    <FormControl fullWidth margin="normal">
                        <InputLabel>Log Level</InputLabel>
                        <Select
                            value={logLevel}
                            onChange={onLogLevelChanged}
                            label="Log Level"
                            inputProps={{
                                "data-testid": "select-id"
                              }}
                        >
                            {supportedLogLevels.map(level => (
                                <MenuItem key={level} value={level} data-testId={level}>
                                    {level}
                                </MenuItem>
                            ))}
                        </Select>
                    </FormControl>
                </Grid>
                <Grid  size={{ xs: 12 }}>
                    <Button onClick={onDownload} variant="outlined" startIcon={<DownloadIcon />}>
                        Download
                    </Button>
                    <Button onClick={refreshLogs} variant="outlined" startIcon={<RefreshIcon />}>
                        Refresh
                    </Button>
                </Grid>
                <Grid  size={{ xs: 12 }}>
                    <LighthouseLogViewer data={logs} />
                </Grid>
            </Grid>
        </InputGroup>
    );
}

export default LogSettings;
