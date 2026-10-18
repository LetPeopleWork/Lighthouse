import { Alert, Button, Container, Link, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useCallback, useContext } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router";
import CreateConnectionWizard from "../../../components/Common/Connection/CreateConnectionWizard";
import ModifyConnectionSettings from "../../../components/Common/Connection/ModifyConnectionSettings";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useRbacGate } from "../../../hooks/useRbacGate";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { UsageDataUpdates } from "../../../models/UsageData/UsageData";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import type { ISaveConnectionChoice } from "../../../services/Api/WorkTrackingSystemService";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	usageDataWorkTrackingSystemFor,
	useUsageDataReporter,
} from "../../../services/UsageData/usageDataReporter";
import { reportFailedNavigation } from "../../../utils/navigation";

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

	const getSupportedSystems = useCallback(
		() => workTrackingSystemService.getWorkTrackingSystems(),
		[workTrackingSystemService],
	);

	const connectionId = isNewConnection ? undefined : Number.parseInt(id, 10);

	// Keyed by the connection, so an answer for a connection somebody has already left never lands
	// on the one they opened since.
	const connectionQuery = useQuery({
		queryKey: ["workTrackingSystemConnection", connectionId],
		queryFn: async (): Promise<IWorkTrackingSystemConnection | null> => {
			const allConnections =
				await workTrackingSystemService.getConfiguredWorkTrackingSystems();
			return allConnections.find((c) => c.id === connectionId) ?? null;
		},
		enabled: !isNewConnection,
		refetchOnWindowFocus: false,
	});
	const { refetch } = connectionQuery;

	const getConnectionSettings = useCallback(async () => {
		const { data, error } = await refetch();
		if (error) {
			throw error;
		}
		return data ?? null;
	}, [refetch]);

	const saveConnectionSettings = async (
		connection: IWorkTrackingSystemConnection,
		choice?: ISaveConnectionChoice,
	) => {
		const isBeingSetUp = isNewConnection && connection.id === 0;

		const saved = isBeingSetUp
			? await workTrackingSystemService.addNewWorkTrackingSystemConnection(
					connection,
				)
			: await workTrackingSystemService.updateWorkTrackingSystemConnection(
					connection,
					choice,
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

	const saveAndNavigate = async (
		connection: IWorkTrackingSystemConnection,
		choice?: ISaveConnectionChoice,
	) => {
		await saveConnectionSettings(connection, choice);
		// A save that asked nothing reports nothing, and neither does one that changed only defaults:
		// a default is not an Additional Field.
		if (choice?.additionalFieldsChanged) {
			reportUsage({
				name: UsageDataEventName.WorkTrackingSystemFieldsChanged,
				updates: choice.startUpdates
					? UsageDataUpdates.Now
					: UsageDataUpdates.Later,
			});
		}
		reportFailedNavigation(navigate("/"));
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

	if (connectionQuery.data === undefined && connectionQuery.isFetching) {
		return (
			<LoadingAnimation isLoading hasError={false}>
				{null}
			</LoadingAnimation>
		);
	}

	if (connectionQuery.isLoadingError) {
		return (
			<Container maxWidth={false}>
				<Alert
					severity="error"
					action={
						<Button
							color="inherit"
							size="small"
							onClick={() => {
								refetch().catch(() => undefined);
							}}
						>
							Try again
						</Button>
					}
				>
					Could not load this connection.
				</Alert>
			</Container>
		);
	}

	return (
		<SnackbarErrorHandler>
			<ModifyConnectionSettings
				key={connectionId}
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
