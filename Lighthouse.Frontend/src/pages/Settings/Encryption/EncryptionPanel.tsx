import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import {
	type EncryptionKeyState,
	KEY_CUSTODY_WORDING,
	type KeyCustody,
} from "../../../models/Encryption/EncryptionKeyState";
import {
	SECRET_OUTCOME_WORDING,
	type SecretReadabilityReport,
	type StoredSecret,
} from "../../../models/Encryption/SecretReadabilityReport";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

// Where the key is not Lighthouse's to replace, the panel says who it belongs to and what that person
// does about it. It never draws a Rotate control that cannot be honoured - not even a disabled one,
// because a control that exists and cannot be used teaches the wrong model of who owns what.
const WHO_OWNS_THE_KEY: Record<KeyCustody, string> = {
	GeneratedForThisInstance:
		"Lighthouse made this key and keeps it, so it can make a new one for you.",
	SuppliedByConfiguration:
		"This key was supplied through the instance's configuration, so it belongs to whoever set it. To replace it, put the new key ahead of the old one in Encryption__Keys, start Lighthouse again, and then move the stored secrets onto it.",
	SuppliedByExternalSecret:
		"This key was supplied from a mounted secret, so it belongs to whoever keeps that secret. To replace it, put the new key ahead of the old one in that secret, restart the pod, and then move the stored secrets onto it.",
	NoDurableStore:
		"This instance has nowhere to keep a key that would still be there after a restart, so it is running on the key published with the product. Set Encryption__Key, or set Encryption__KeyStorePath to a directory that outlives this container, and start Lighthouse again.",
};

const KeyRing: React.FC<{ keyState: EncryptionKeyState }> = ({ keyState }) => (
	<TableContainer>
		<Table data-testid="encryption-key-ring">
			<TableBody>
				<TableRow>
					<TableCell>Key source</TableCell>
					<TableCell data-testid="encryption-custody">
						{KEY_CUSTODY_WORDING[keyState.custody]}
					</TableCell>
				</TableRow>
				<TableRow>
					<TableCell>Active key</TableCell>
					<TableCell data-testid="encryption-active-key-id">
						{keyState.activeKeyId}
					</TableCell>
				</TableRow>
				<TableRow>
					<TableCell>Keys held</TableCell>
					<TableCell>
						<Stack
							direction="row"
							spacing={1}
							useFlexGap
							sx={{ flexWrap: "wrap" }}
						>
							{keyState.keyIds.map((keyId) => (
								<Chip
									key={keyId}
									label={keyId}
									size="small"
									data-testid={`encryption-key-${keyId}`}
								/>
							))}
						</Stack>
					</TableCell>
				</TableRow>
				<TableRow>
					<TableCell>Kept in</TableCell>
					<TableCell data-testid="encryption-key-store-path">
						{keyState.keyStorePath}
					</TableCell>
				</TableRow>
			</TableBody>
		</Table>
	</TableContainer>
);

// A secret that moved needs no listing - the count already says so. What an operator has to act on is
// what was left behind, and the only useful thing to say about it is which Connection and which field.
const wasLeftBehind = (secret: StoredSecret): boolean =>
	secret.outcome !== "Moved" &&
	secret.outcome !== "Unmoved" &&
	secret.outcome !== "MovedByAnotherWriter";

const WhatHappened: React.FC<{ report: SecretReadabilityReport }> = ({
	report,
}) => {
	const leftBehind = report.secrets.filter(wasLeftBehind);

	return (
		<Box sx={{ mt: 2 }} data-testid="encryption-report">
			<Alert severity={leftBehind.length > 0 ? "warning" : "success"}>
				{`Moved ${report.movedCount} stored secrets onto key ${report.activeKeyId}. ${report.unreadableCount} could not be read.`}
			</Alert>

			{leftBehind.length > 0 && (
				<TableContainer sx={{ mt: 2 }}>
					<Table size="small" data-testid="encryption-report-secrets">
						<TableHead>
							<TableRow>
								<TableCell>Connection</TableCell>
								<TableCell>Field</TableCell>
								<TableCell>What happened</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{leftBehind.map((secret) => (
								<TableRow key={`${secret.connectionId}-${secret.field}`}>
									<TableCell>{secret.connectionName}</TableCell>
									<TableCell>{secret.field}</TableCell>
									<TableCell>
										{SECRET_OUTCOME_WORDING[secret.outcome]}
									</TableCell>
								</TableRow>
							))}
						</TableBody>
					</Table>
				</TableContainer>
			)}
		</Box>
	);
};

const EncryptionPanel: React.FC = () => {
	const [keyState, setKeyState] = useState<EncryptionKeyState | null>(null);
	const [report, setReport] = useState<SecretReadabilityReport | null>(null);
	const [failure, setFailure] = useState<string | null>(null);
	const [busy, setBusy] = useState(false);

	const { encryptionService } = useContext(ApiServiceContext);

	const readKeyState = useCallback(async () => {
		try {
			setKeyState(await encryptionService.getKeyState());
		} catch {
			setKeyState(null);
		}
	}, [encryptionService]);

	useEffect(() => {
		readKeyState();
	}, [readKeyState]);

	const run = async (
		action: () => Promise<SecretReadabilityReport>,
	): Promise<void> => {
		setBusy(true);
		setFailure(null);

		try {
			setReport(await action());
			await readKeyState();
		} catch (caught) {
			setFailure(
				caught instanceof Error
					? caught.message
					: "The stored secrets could not be moved.",
			);
		} finally {
			setBusy(false);
		}
	};

	if (keyState === null) {
		return null;
	}

	return (
		<InputGroup title="Secret Encryption Key" initiallyExpanded={true}>
			<KeyRing keyState={keyState} />

			<Typography
				variant="body2"
				sx={{ mt: 2 }}
				data-testid="encryption-custody-explanation"
			>
				{WHO_OWNS_THE_KEY[keyState.custody]}
			</Typography>

			<Stack direction="row" spacing={2} sx={{ mt: 2 }}>
				{keyState.canMint && (
					<Button
						variant="contained"
						disabled={busy}
						data-testid="rotate-key-button"
						onClick={() => run(() => encryptionService.rotateKey())}
					>
						Rotate key
					</Button>
				)}
				<Button
					variant={keyState.canMint ? "outlined" : "contained"}
					disabled={busy}
					data-testid="reencrypt-button"
					onClick={() => run(() => encryptionService.reEncryptSecrets())}
				>
					Move stored secrets onto the active key
				</Button>
			</Stack>

			{failure !== null && (
				<Alert severity="error" sx={{ mt: 2 }} data-testid="encryption-failure">
					{failure}
				</Alert>
			)}

			{report !== null && <WhatHappened report={report} />}
		</InputGroup>
	);
};

export default EncryptionPanel;
