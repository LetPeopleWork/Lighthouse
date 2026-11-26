import {
	Box,
	Button,
	Paper,
	Stack,
	TextField,
	Typography,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import type { IReportOwner } from "../../../models/Reports/Report";
import type { IReportNote } from "../../../models/Reports/ReportNote";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import { WidgetBody } from "../MetricsView/WidgetShell";
import { queryStatusOf } from "../MetricsView/widgetStatus";
import { reportNotesQueryKeyOf, useReportNotes } from "./useReportNotes";

const NEEDS_TEXT = "A note needs some text.";
const AT_MOST_2000 = "A note can have at most 2000 characters.";
const COULD_NOT_SAVE = "This note couldn't be saved. Try again.";

const refusalCopyOf = (error: unknown): string => {
	if (error instanceof ApiError) {
		if (error.problemCode === "note-text-missing") return NEEDS_TEXT;
		if (error.problemCode === "note-text-too-long") return AT_MOST_2000;
	}
	return COULD_NOT_SAVE;
};

/** The line under a note's text: the day it was written, then who wrote it when somebody signed it. */
export const bylineOf = (note: IReportNote): string => {
	const written = note.authorDisplayName
		? `${note.createdOn} · ${note.authorDisplayName}`
		: note.createdOn;
	return note.lastEditedOn
		? `${written} · edited ${note.lastEditedOn}`
		: written;
};

const ReportNotes: React.FC<
	Readonly<{ owner: IReportOwner; reportId: number; mayWrite: boolean }>
> = ({ owner, reportId, mayWrite }) => {
	const { reportService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	const reportUsage = useUsageDataReporter();
	const notes = useReportNotes(owner, reportId);
	const [text, setText] = useState("");
	const [refusal, setRefusal] = useState<string | undefined>();
	const [isSaving, setIsSaving] = useState(false);

	const status = queryStatusOf(notes.isFetching, notes.isError);
	const listed = notes.isError ? undefined : notes.data;

	const save = async () => {
		if (isSaving) {
			return;
		}
		setIsSaving(true);
		setRefusal(undefined);
		try {
			const saved = await reportService.addNote(owner, reportId, text);
			queryClient.setQueryData<IReportNote[]>(
				reportNotesQueryKeyOf(owner, reportId),
				(held) => [saved, ...(held ?? [])],
			);
			reportUsage({ name: UsageDataEventName.ReportNoteAdded });
			setText("");
		} catch (error_) {
			setRefusal(refusalCopyOf(error_));
		} finally {
			setIsSaving(false);
		}
	};

	return (
		<Paper variant="outlined" sx={{ p: 2 }} data-testid="report-notes">
			<Stack spacing={1.5}>
				<Typography variant="h6" component="h2">
					Notes
				</Typography>
				<WidgetBody
					widgetKey="report-notes"
					status={status}
					hasContentToDim={listed !== undefined}
					loadingMessage="Loading notes"
					failureMessage="Could not load the notes"
					onRetry={() => notes.refetch()}
				>
					{listed !== undefined && (
						<Stack spacing={1.5}>
							{listed.length === 0 && (
								<Typography variant="body2" color="text.secondary">
									No notes yet.
								</Typography>
							)}
							{listed.map((note) => (
								<Box key={note.id} data-testid="report-note">
									<Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
										{note.text}
									</Typography>
									<Typography variant="caption" color="text.secondary">
										{bylineOf(note)}
									</Typography>
								</Box>
							))}
							{mayWrite && (
								<Stack spacing={1} sx={{ alignItems: "flex-start" }}>
									<TextField
										multiline
										minRows={2}
										fullWidth
										label="Note"
										value={text}
										onChange={(event) => setText(event.target.value)}
										error={refusal !== undefined}
										helperText={refusal}
										slotProps={{
											htmlInput: { "data-testid": "report-note-input" },
										}}
									/>
									<Button
										variant="contained"
										size="small"
										disabled={isSaving}
										onClick={save}
										data-testid="save-report-note-button"
									>
										Add note
									</Button>
								</Stack>
							)}
						</Stack>
					)}
				</WidgetBody>
			</Stack>
		</Paper>
	);
};

export default ReportNotes;
