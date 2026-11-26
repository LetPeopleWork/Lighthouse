import { Box, Button, Stack, TextField, Typography } from "@mui/material";
import type React from "react";
import { useState } from "react";
import type { IReportNote } from "../../../models/Reports/ReportNote";
import { bylineOf } from "./reportNoteByline";
import { refusalCopyOf } from "./reportNoteRefusalCopy";

const COULD_NOT_DELETE = "This note couldn't be deleted. Try again.";

type ReportNoteItemProps = Readonly<{
	note: IReportNote;
	canChange: boolean;
	onChange: (text: string) => Promise<void>;
	onDelete: () => Promise<void>;
}>;

const ReportNoteItem: React.FC<ReportNoteItemProps> = ({
	note,
	canChange,
	onChange,
	onDelete,
}) => {
	const [draft, setDraft] = useState<string | undefined>();
	const [problem, setProblem] = useState<string | undefined>();
	const [isBusy, setIsBusy] = useState(false);

	const attempt = async (
		work: () => Promise<void>,
		copyOf: (error: unknown) => string,
	) => {
		if (isBusy) {
			return;
		}
		setIsBusy(true);
		setProblem(undefined);
		try {
			await work();
		} catch (error_) {
			setProblem(copyOf(error_));
		} finally {
			setIsBusy(false);
		}
	};

	const save = () =>
		attempt(async () => {
			await onChange(draft ?? "");
			setDraft(undefined);
		}, refusalCopyOf);

	const isEditing = draft !== undefined;

	return (
		<Box data-testid="report-note">
			{isEditing ? (
				<TextField
					multiline
					minRows={2}
					fullWidth
					value={draft}
					onChange={(event) => setDraft(event.target.value)}
					error={problem !== undefined}
					helperText={problem}
					slotProps={{ htmlInput: { "aria-label": "Note text" } }}
				/>
			) : (
				<Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
					{note.text}
				</Typography>
			)}
			<Typography variant="caption" color="text.secondary">
				{bylineOf(note)}
			</Typography>
			{canChange && !isEditing && (
				<Stack direction="row" spacing={1}>
					<Button size="small" onClick={() => setDraft(note.text)}>
						Edit
					</Button>
					<Button
						size="small"
						color="error"
						disabled={isBusy}
						onClick={() => attempt(onDelete, () => COULD_NOT_DELETE)}
					>
						Delete
					</Button>
				</Stack>
			)}
			{isEditing && (
				<Stack direction="row" spacing={1}>
					<Button
						size="small"
						variant="contained"
						disabled={isBusy}
						onClick={save}
					>
						Save
					</Button>
					<Button
						size="small"
						onClick={() => {
							setDraft(undefined);
							setProblem(undefined);
						}}
					>
						Cancel
					</Button>
				</Stack>
			)}
			{!isEditing && problem !== undefined && (
				<Typography variant="caption" color="error" role="alert">
					{problem}
				</Typography>
			)}
		</Box>
	);
};

export default ReportNoteItem;
