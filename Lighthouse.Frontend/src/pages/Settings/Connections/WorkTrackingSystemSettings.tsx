import Container from "@mui/material/Container";
import FormControlLabel from "@mui/material/FormControlLabel";
import Grid from "@mui/material/Grid";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import type { IWorkTrackingSystemSettings } from "../../../models/AppSettings/WorkTrackingSystemSettings";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const WorkTrackingSystemSettings: React.FC = () => {
	const [settings, setSettings] = useState<IWorkTrackingSystemSettings | null>(
		null,
	);
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const [hasError, setHasError] = useState<boolean>(false);

	const { settingsService } = useContext(ApiServiceContext);

	const updateSettings = async () => {
		if (settings == null) {
			return;
		}

		await settingsService.updateWorkTrackingSystemSettings(settings);
	};

	useEffect(() => {
		const fetchData = async () => {
			setIsLoading(true);
			setHasError(false);

			try {
				const loadedSettings =
					await settingsService.getWorkTrackingSystemSettings();
				setSettings(loadedSettings);
			} catch {
				setHasError(true);
			} finally {
				setIsLoading(false);
			}
		};

		fetchData();
	}, [settingsService]);

	const handleSwitchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		if (settings) {
			setSettings({
				...settings,
				overrideRequestTimeout: event.target.checked,
			});
		}
	};

	const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		if (settings) {
			setSettings({
				...settings,
				requestTimeoutInSeconds: Number.parseInt(event.target.value, 10),
			});
		}
	};

	return (
		<InputGroup title={"Work Tracking System Settings"}>
			<LoadingAnimation isLoading={isLoading} hasError={hasError}>
				<Container maxWidth={false}>
					<Grid container spacing={3}>
						<Grid size={{ xs: 12 }}>
							<FormControlLabel
								control={
									<Switch
										checked={settings?.overrideRequestTimeout ?? false}
										onChange={handleSwitchChange}
										data-testid="override-request-timeout"
									/>
								}
								label="Override Request Timeout"
							/>
						</Grid>
						<Grid size={{ xs: 12 }}>
							<TextField
								label="Request Timeout (Seconds)"
								type="number"
								data-testid="request-timeout"
								value={settings?.requestTimeoutInSeconds ?? ""}
								onChange={handleInputChange}
								fullWidth
								disabled={!settings?.overrideRequestTimeout}
								slotProps={{
									htmlInput: {
										min: 1,
									},
								}}
							/>
						</Grid>
						<Grid size={{ xs: 12 }}>
							<ActionButton
								buttonVariant="contained"
								onClickHandler={updateSettings}
								buttonText="Update Work Tracking System Settings"
							/>
						</Grid>
					</Grid>
				</Container>
			</LoadingAnimation>
		</InputGroup>
	);
};

export default WorkTrackingSystemSettings;
