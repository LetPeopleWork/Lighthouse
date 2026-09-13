import { Alert, Container, Link, Typography } from "@mui/material";
import type React from "react";
import { useContext } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router";
import CreateConnectionWizard from "../../../components/Common/Connection/CreateConnectionWizard";
import ModifyConnectionSettings from "../../../components/Common/Connection/ModifyConnectionSettings";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useRbacGate } from "../../../hooks/useRbacGate";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	usageDataWorkTrackingSystemFor,
	useUsageDataReporter,
} from "../../../services/UsageData/usageDataReporter";

const EditConnectionPage: React.FC = () => {
	const { id } = useParams<{ id?: string }>();
	const isNewConnection = id === undefined;
	const navigate = useNavigate();
	const gate = useRbacGate({ kind: "systemAdmin" });

	const { getTerm } = useTerminology();
	const connectionTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);

	const pageTitle = isNewConnection
		? `Create ${connectionTerm} Connection`
		: `Update ${connectionTerm} Connection`;

	const { workTrackingSystemService } = useContext(ApiServiceContext);
	const reportUsage = useUsageDataReporter();

	const getSupportedSystems = async () => {
		return await workTrackingSystemService.getWorkTrackingSystems();
	};

	const getConnectionSettings =
		async (): Promise<IWorkTrackingSystemConnection | null> => {
			if (isNewConnection) {
				return null;
			}

			const allConnections =
				await workTrackingSystemService.getConfiguredWorkTrackingSystems();
			const connectionId = Number.parseInt(id, 10);
			return allConnections.find((c) => c.id === connectionId) ?? null;
		};

	const saveConnectionSettings = async (
		connection: IWorkTrackingSystemConnection,
	) => {
		const isBeingSetUp = isNewConnection && connection.id === 0;

		const saved = isBeingSetUp
			? await workTrackingSystemService.addNewWorkTrackingSystemConnection(
					connection,
				)
			: await workTrackingSystemService.updateWorkTrackingSystemConnection(
					connection,
				);

		// Only a connection being set up, never one being edited. Somebody changing a password on a
		// connection they have had for a year has not adopted anything.
		if (isBeingSetUp) {
			reportUsage({
				name: UsageDataEventName.WorkTrackingSystemConnected,
				workTrackingSystem: usageDataWorkTrackingSystemFor(
					saved.workTrackingSystem,
				),
			});
		}

		return saved;
	};

	const saveAndNavigate = async (connection: IWorkTrackingSystemConnection) => {
		await saveConnectionSettings(connection);
		navigate("/");
	};

	const validateConnectionSettings = async (
		connection: IWorkTrackingSystemConnection,
	) => {
		return await workTrackingSystemService.validateWorkTrackingSystemConnection(
			connection,
		);
	};

	if (gate.isLoading) {
		return null;
	}

	if (!gate.allowed) {
		return (
			<Container maxWidth={false}>
				<Alert
					severity="info"
					sx={{ mb: 2 }}
					data-testid="connection-edit-no-access-alert"
				>
					You don't have permission to access this page.{" "}
					<Link component={RouterLink} to="/">
						Back to Overview
					</Link>
				</Alert>
			</Container>
		);
	}

	if (isNewConnection) {
		return (
			<SnackbarErrorHandler>
				<Container maxWidth={false}>
					<Typography variant="h4" sx={{ mb: 2 }}>
						{pageTitle}
					</Typography>
					<CreateConnectionWizard
						getSupportedSystems={getSupportedSystems}
						validateConnection={validateConnectionSettings}
						saveConnection={saveConnectionSettings}
						onCancel={() => navigate("/")}
						onComplete={() => navigate("/")}
					/>
				</Container>
			</SnackbarErrorHandler>
		);
	}

	return (
		<SnackbarErrorHandler>
			<ModifyConnectionSettings
				title={pageTitle}
				getSupportedSystems={getSupportedSystems}
				getConnectionSettings={getConnectionSettings}
				saveConnectionSettings={saveAndNavigate}
				validateConnectionSettings={validateConnectionSettings}
			/>
		</SnackbarErrorHandler>
	);
};

export default EditConnectionPage;
