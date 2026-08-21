import { Box, Button, Stack, TextField, Typography } from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import type { DeliveryNote } from "../../../../../models/Delivery/DeliveryNote";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";

interface DeliveryNotesPanelProps {
	deliveryId: number;
	canWrite: boolean;
}

const DeliveryNotesPanel: React.FC<DeliveryNotesPanelProps> = ({
	deliveryId,
	canWrite,
}) => {
	const { deliveryService } = useContext(ApiServiceContext);
	const [notes, setNotes] = useState<DeliveryNote[]>([]);
	const [text, setText] = useState("");
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(true);
	const [isSaving, setIsSaving] = useState(false);
	const [editingId, setEditingId] = useState<number | null>(null);
	const [editText, setEditText] = useState("");
	const [editError, setEditError] = useState<string | null>(null);

	const loadNotes = useCallback(async () => {
		try {
			setNotes(await deliveryService.getNotes(deliveryId));
		} catch {
			setNotes([]);
		} finally {
			setIsLoading(false);
		}
	}, [deliveryService, deliveryId]);

	useEffect(() => {
		loadNotes();
	}, [loadNotes]);

	const saveNote = async () => {
		if (text.trim().length === 0) {
			setError("A note needs some text.");
			return;
		}

		setIsSaving(true);
		try {
			const saved = await deliveryService.addNote(deliveryId, text);
			setNotes((existing) => [saved, ...existing]);
			setText("");
			setError(null);
		} catch {
			setError("The note could not be saved.");
		} finally {
			setIsSaving(false);
		}
	};

	const startEdit = (noteId: number, currentText: string) => {
		setEditingId(noteId);
		setEditText(currentText);
		setEditError(null);
	};

	const cancelEdit = () => {
		setEditingId(null);
		setEditText("");
		setEditError(null);
	};

	const saveEdit = async (noteId: number) => {
		if (editText.trim().length === 0) {
			setEditError("A note needs some text.");
			return;
		}

		try {
			const updated = await deliveryService.updateNote(
				deliveryId,
				noteId,
				editText,
			);
			setNotes((existing) =>
				existing.map((note) => (note.id === noteId ? updated : note)),
			);
			cancelEdit();
		} catch {
			setEditError("The note could not be saved.");
		}
	};

	const removeNote = async (noteId: number) => {
		try {
			await deliveryService.deleteNote(deliveryId, noteId);
			setNotes((existing) => existing.filter((note) => note.id !== noteId));
		} catch {
			setError("The note could not be withdrawn.");
		}
	};

	return (
		<Box sx={{ p: 2 }} data-testid="delivery-notes-panel">
			{canWrite && (
				<Stack spacing={1} sx={{ mb: 2 }}>
					<TextField
						label="Add a note"
						value={text}
						onChange={(event) => {
							setText(event.target.value);
							if (error) setError(null);
						}}
						error={error !== null}
						helperText={error ?? " "}
						multiline
						minRows={2}
						fullWidth
						slotProps={{ htmlInput: { "data-testid": "note-input" } }}
					/>
					<Box sx={{ display: "flex", justifyContent: "flex-end" }}>
						<Button
							variant="contained"
							onClick={saveNote}
							disabled={isSaving}
							data-testid="save-note-button"
						>
							Save
						</Button>
					</Box>
				</Stack>
			)}

			{!isLoading && notes.length === 0 && (
				<Typography variant="body2" color="text.secondary">
					No notes yet.
				</Typography>
			)}

			<Stack spacing={2}>
				{notes.map((note) => (
					<Box key={note.id} data-testid="delivery-note">
						{editingId === note.id ? (
							<Stack spacing={1}>
								<TextField
									value={editText}
									onChange={(event) => {
										setEditText(event.target.value);
										if (editError) setEditError(null);
									}}
									error={editError !== null}
									helperText={editError ?? " "}
									multiline
									minRows={2}
									fullWidth
									slotProps={{
										htmlInput: { "data-testid": "edit-note-input" },
									}}
								/>
								<Box
									sx={{ display: "flex", gap: 1, justifyContent: "flex-end" }}
								>
									<Button onClick={cancelEdit} data-testid="cancel-edit-button">
										Cancel
									</Button>
									<Button
										variant="contained"
										onClick={() => saveEdit(note.id)}
										data-testid="save-edit-button"
									>
										Save
									</Button>
								</Box>
							</Stack>
						) : (
							<>
								{/* Whitespace is preserved because a line break somebody typed is part of
								    what they wrote; the text renders as characters, so markup shows as
								    itself. */}
								<Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
									{note.text}
								</Typography>
								<Box
									sx={{
										display: "flex",
										alignItems: "center",
										gap: 1,
										mt: 0.5,
									}}
								>
									<Typography variant="caption" color="text.secondary">
										{note.authorDisplayName
											? `${note.createdOn} · ${note.authorDisplayName}`
											: note.createdOn}
										{note.lastEditedOn ? ` · edited ${note.lastEditedOn}` : ""}
									</Typography>
									{note.canModify && (
										<>
											<Button
												size="small"
												onClick={() => startEdit(note.id, note.text)}
												data-testid="edit-note-button"
											>
												Edit
											</Button>
											<Button
												size="small"
												color="error"
												onClick={() => removeNote(note.id)}
												data-testid="delete-note-button"
											>
												Delete
											</Button>
										</>
									)}
								</Box>
							</>
						)}
					</Box>
				))}
			</Stack>
		</Box>
	);
};

export default DeliveryNotesPanel;
