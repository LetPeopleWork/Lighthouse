import ArticleIcon from "@mui/icons-material/Article";
import DatasetIcon from "@mui/icons-material/Dataset";
import KeyIcon from "@mui/icons-material/Key";
import LockIcon from "@mui/icons-material/Lock";
import ManageAccountsIcon from "@mui/icons-material/ManageAccounts";
import SettingsSystemDaydreamIcon from "@mui/icons-material/SettingsSystemDaydream";
import StorageIcon from "@mui/icons-material/Storage";
import type React from "react";
import type { SettingsTabSlug } from "../../models/Settings/settingsAreas";
import ApiKeysSettings from "./ApiKeys/ApiKeysSettings";
import DatabaseManagementSettings from "./DatabaseManagement/DatabaseManagementSettings";
import DemoDataSettings from "./DemoData/DemoDataSettings";
import EncryptionPanel from "./Encryption/EncryptionPanel";
import RbacSettings from "./Rbac/RbacSettings";
import SystemSettingsTab from "./System/SystemSettingsTab";
import SystemInfoSettings from "./SystemInfo/SystemInfoSettings";

interface TabPresentation {
	readonly icon: React.ReactElement;
	readonly panel: React.ReactElement;
}

/** What each tab of the definition looks like and which panel it opens. */
export const settingsPanels: Readonly<
	Record<SettingsTabSlug, TabPresentation>
> = {
	configuration: {
		icon: <SettingsSystemDaydreamIcon />,
		panel: <SystemSettingsTab />,
	},
	"system-wide": { icon: <ManageAccountsIcon />, panel: <RbacSettings /> },
	"api-keys": { icon: <KeyIcon />, panel: <ApiKeysSettings /> },
	"demo-data": { icon: <DatasetIcon />, panel: <DemoDataSettings /> },
	database: {
		icon: <StorageIcon />,
		panel: <DatabaseManagementSettings />,
	},
	encryption: { icon: <LockIcon />, panel: <EncryptionPanel /> },
	"system-info": { icon: <ArticleIcon />, panel: <SystemInfoSettings /> },
};
