import type { RefreshLog } from "../../models/SystemInfo/RefreshLog";
import type { SystemInfo } from "../../models/SystemInfo/SystemInfo";
import { BaseApiService } from "./BaseApiService";

export type SpdxExternalRef = {
	referenceCategory: string;
	referenceType: string;
	referenceLocator: string;
};

export type SpdxPackage = {
	name: string;
	SPDXID: string;
	versionInfo: string;
	licenseDeclared: string;
	externalRefs?: SpdxExternalRef[];
};

export type SpdxDocument = {
	packages: SpdxPackage[];
	documentDescribes: string[];
};

export type CdxLicense = {
	license: { id: string };
};

export type CdxComponent = {
	name: string;
	version: string;
	purl?: string;
	scope?: string;
	licenses?: CdxLicense[];
};

export type CycloneDxDocument = {
	components: CdxComponent[];
	metadata?: { component?: { name: string } };
};

function isSpdxDocument(data: unknown): data is SpdxDocument {
	if (typeof data !== "object" || data === null) return false;
	const doc = data as Record<string, unknown>;
	return Array.isArray(doc.packages) && Array.isArray(doc.documentDescribes);
}

function isCycloneDxDocument(data: unknown): data is CycloneDxDocument {
	if (typeof data !== "object" || data === null) return false;
	const doc = data as Record<string, unknown>;
	return Array.isArray(doc.components);
}

export interface ISystemInfoService {
	getSystemInfo(): Promise<SystemInfo>;
	getRefreshLogs(): Promise<RefreshLog[]>;
	getBackendSbom(): Promise<SpdxDocument>;
	getFrontendSbom(): Promise<CycloneDxDocument>;
}

export class SystemInfoService
	extends BaseApiService
	implements ISystemInfoService
{
	async getSystemInfo(): Promise<SystemInfo> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<SystemInfo>("/systeminfo");
			return response.data;
		});
	}

	async getRefreshLogs(): Promise<RefreshLog[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RefreshLog[]>(
				"/systeminfo/refreshlog",
			);
			return response.data;
		});
	}

	async getBackendSbom(): Promise<SpdxDocument> {
		const response = await fetch("/sbom/backend.spdx.json");
		if (!response.ok) {
			throw new Error(`Failed to load backend SBOM: ${response.status}`);
		}
		const data: unknown = await response.json();
		if (!isSpdxDocument(data)) {
			throw new Error("Invalid SPDX document format");
		}
		return data;
	}

	async getFrontendSbom(): Promise<CycloneDxDocument> {
		const response = await fetch("/sbom/frontend.cdx.json");
		if (!response.ok) {
			throw new Error(`Failed to load frontend SBOM: ${response.status}`);
		}
		const data: unknown = await response.json();
		if (!isCycloneDxDocument(data)) {
			throw new Error("Invalid CycloneDX document format");
		}
		return data;
	}
}
