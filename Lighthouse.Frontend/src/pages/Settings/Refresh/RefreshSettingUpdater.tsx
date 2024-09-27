import React, { useContext, useEffect, useState } from "react";
import { IRefreshSettings } from "../../../models/AppSettings/RefreshSettings";
import { Container, Grid, TextField } from "@mui/material";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface RefreshSettingUpdaterProps {
    settingName: string;
}

const RefreshSettingUpdater: React.FC<RefreshSettingUpdaterProps> = ({ settingName }) => {
    const [refreshSettings, setRefreshSettings] = useState<IRefreshSettings | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);

    const { settingsService } = useContext(ApiServiceContext);

    const updateSettings = async () => {
        if (refreshSettings == null) {
            return;
        }

        await settingsService.updateRefreshSettings(settingName, refreshSettings);
    };

    const fetchData = async () => {
        setIsLoading(true);
        setHasError(false);

        try {
            const loadedSettings = await settingsService.getRefreshSettings(settingName);
            setRefreshSettings(loadedSettings);
        } catch {
            setHasError(true);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchData();
    }, []);

    const handleInputChange = (field: keyof IRefreshSettings) => (event: React.ChangeEvent<HTMLInputElement>) => {
        if (refreshSettings) {
            setRefreshSettings({
                ...refreshSettings,
                [field]: parseInt(event.target.value, 10),
            });
        }
    };

    return (
        <LoadingAnimation isLoading={isLoading} hasError={hasError}>
            <Container>
                <Grid container spacing={3}>
                    <Grid item xs={12}>
                        <TextField
                            label="Interval (Minutes)"
                            type="number"
                            value={refreshSettings?.interval ?? ""}
                            onChange={handleInputChange("interval")}
                            fullWidth
                            InputProps={{
                                inputProps: {
                                    min: 1
                                }
                            }}
                        />
                    </Grid>
                    <Grid item xs={12}>
                        <TextField
                            label="Refresh After (Minutes)"
                            type="number"
                            value={refreshSettings?.refreshAfter ?? ""}
                            onChange={handleInputChange("refreshAfter")}
                            fullWidth
                            InputProps={{
                                inputProps: {
                                    min: 1
                                }
                            }}
                        />
                    </Grid>
                    <Grid item xs={12}>
                        <TextField
                            label="Start Delay (Minutes)"
                            type="number"
                            value={refreshSettings?.startDelay ?? ""}
                            onChange={handleInputChange("startDelay")}
                            fullWidth
                            InputProps={{
                                inputProps: {
                                    min: 1
                                }
                            }}
                        />
                    </Grid>
                    <Grid item xs={12}>
                        <ActionButton
                            buttonVariant="contained"
                            onClickHandler={updateSettings}
                            buttonText={`Update ${settingName} Settings`} />
                    </Grid>
                </Grid>
            </Container>
        </LoadingAnimation>
    );
};

export default RefreshSettingUpdater;