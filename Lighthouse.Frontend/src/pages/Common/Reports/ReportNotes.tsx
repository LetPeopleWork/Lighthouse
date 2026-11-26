import { Button, Paper, Stack, TextField, Typography } from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import type { IReportOwner } from "../../../models/Reports/Report";
import type { IReportNote } from "../../../models/Reports/ReportNote";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import { WidgetBody } from "../MetricsView/WidgetShell";
import { queryStatusOf } from "../MetricsView/widgetStatus";
import ReportNoteItem from "./ReportNoteItem";
import { refusalCopyOf } from "./reportNoteRefusalCopy";
import { reportNotesQueryKeyOf, useReportNotes } from "./useReportNotes";

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

	const keepNotes = (change: (held: IReportNote[]) => IReportNote[]) =>
		queryClient.setQueryData<IReportNote[]>(
			reportNotesQueryKeyOf(owner, reportId),
			(held) => change(held ?? []),
		);

	const change = async (noteId: number, changedText: string) => {
		const changed = await reportService.updateNote(
			owner,
			reportId,
			noteId,
			changedText,
		);
		keepNotes((held) =>
			held.map((kept) => (kept.id === noteId ? changed : kept)),
		);
	};

	const remove = async (noteId: number) => {
		await reportService.deleteNote(owner, reportId, noteId);
		keepNotes((held) => held.filter((kept) => kept.id !== noteId));
	};

	const save = async () => {
		if (isSaving) {
			return;
		}
		setIsSaving(true);
		setRefusal(undefined);
		try {
			const saved = await reportService.addNote(owner, reportId, text);
			keepNotes((held) => [saved, ...held]);
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
								<ReportNoteItem
									key={note.id}
									note={note}
									canChange={mayWrite && note.canModify === true}
									onChange={(changedText) => change(note.id, changedText)}
									onDelete={() => remove(note.id)}
								/>
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
