import FolderSpecialIcon from "@mui/icons-material/FolderSpecial";
import GroupsIcon from "@mui/icons-material/Groups";
import type React from "react";
import type { IReportOwner } from "../../../models/Reports/Report";

/** Tells a Team from a Portfolio at a glance; `title` names the kind to assistive technology where nothing else does. */
export const OwnerKindIcon: React.FC<
	Readonly<{ kind: IReportOwner["kind"]; title?: string }>
> = ({ kind, title }) => {
	const KindIcon = kind === "team" ? GroupsIcon : FolderSpecialIcon;
	return <KindIcon fontSize="small" color="action" titleAccess={title} />;
};
