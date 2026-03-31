import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Link from "@mui/material/Link";
import TextField from "@mui/material/TextField";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useContext, useEffect, useMemo, useState } from "react";
import DataGridBase from "../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import type { ThirdPartyPackage } from "../../../models/SystemInfo/ThirdPartyPackage";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type {
	CycloneDxDocument,
	SpdxDocument,
} from "../../../services/Api/SystemInfoService";

function purlToPackageUrl(purl: string | undefined): string | null {
	if (!purl) return null;

	const nugetMatch = /^pkg:nuget\/([^@]+)@(.+)$/.exec(purl);
	if (nugetMatch) {
		return `https://www.nuget.org/packages/${nugetMatch[1]}/${nugetMatch[2]}`;
	}

	const npmMatch = /^pkg:npm\/([^@]+|%40[^@]+)@(.+)$/.exec(purl);
	if (npmMatch) {
		const name = decodeURIComponent(npmMatch[1]);
		return `https://www.npmjs.com/package/${name}/v/${npmMatch[2]}`;
	}

	return null;
}

function parseSpdxPackages(doc: SpdxDocument): ThirdPartyPackage[] {
	return doc.packages
		.filter((pkg) => pkg.SPDXID !== "SPDXRef-RootPackage")
		.map((pkg) => {
			const purlRef = pkg.externalRefs?.find(
				(ref) => ref.referenceType === "purl",
			);
			return {
				id: `Backend:${pkg.name}@${pkg.versionInfo}`,
				name: pkg.name,
				version: pkg.versionInfo,
				packageUrl: purlToPackageUrl(purlRef?.referenceLocator),
			};
		});
}

function parseCdxComponents(doc: CycloneDxDocument): ThirdPartyPackage[] {
	const rootName = doc.metadata?.component?.name;
	return doc.components
		.filter((comp) => {
			if (comp.name === rootName) return false;
			if (comp.scope === "optional" || comp.scope === "excluded") return false;
			return true;
		})
		.map((comp) => ({
			id: `Frontend:${comp.name}@${comp.version}`,
			name: comp.name,
			version: comp.version,
			packageUrl: purlToPackageUrl(comp.purl),
		}));
}

const ThirdPartyPackagesSection: React.FC = () => {
	const [packages, setPackages] = useState<ThirdPartyPackage[]>([]);
	const [loading, setLoading] = useState(true);
	const [error, setError] = useState<string | null>(null);
	const [searchText, setSearchText] = useState("");
	const { systemInfoService } = useContext(ApiServiceContext);

	useEffect(() => {
		const loadSbomData = async () => {
			try {
				const [backendSbom, frontendSbom] = await Promise.all([
					systemInfoService.getBackendSbom(),
					systemInfoService.getFrontendSbom(),
				]);
				const backendPackages = parseSpdxPackages(backendSbom);
				const frontendPackages = parseCdxComponents(frontendSbom);
				setPackages([...backendPackages, ...frontendPackages]);
			} catch (err) {
				setError(
					err instanceof Error
						? err.message
						: "Failed to load package information",
				);
			} finally {
				setLoading(false);
			}
		};
		loadSbomData();
	}, [systemInfoService]);

	const filteredPackages = useMemo(() => {
		if (!searchText.trim()) return packages;
		const lower = searchText.toLowerCase();
		return packages.filter(
			(pkg) =>
				pkg.name.toLowerCase().includes(lower) ||
				pkg.version.toLowerCase().includes(lower),
		);
	}, [packages, searchText]);

	const columns: DataGridColumn<ThirdPartyPackage & GridValidRowModel>[] =
		useMemo(
			() => [
				{
					field: "name",
					headerName: "Name",
					flex: 1,
					hideable: false,
					sortable: true,
					renderCell: ({ row }) =>
						row.packageUrl ? (
							<Link
								href={row.packageUrl}
								target="_blank"
								rel="noopener noreferrer"
							>
								{row.name}
							</Link>
						) : (
							row.name
						),
				},
				{
					field: "version",
					headerName: "Version",
					width: 150,
					sortable: true,
				},
			],
			[],
		);

	if (loading) {
		return (
			<Box sx={{ display: "flex", justifyContent: "center", p: 3 }}>
				<CircularProgress />
			</Box>
		);
	}

	if (error) {
		return <Alert severity="error">{error}</Alert>;
	}

	return (
		<Box>
			<TextField
				size="small"
				placeholder="Search packages..."
				value={searchText}
				onChange={(e) => setSearchText(e.target.value)}
				sx={{ mb: 2, width: "100%", maxWidth: 400 }}
			/>
			<DataGridBase
				rows={filteredPackages}
				columns={columns}
				storageKey="third-party-packages-grid"
				hidePagination={false}
				initialSortModel={[{ field: "name", sort: "asc" }]}
			/>
		</Box>
	);
};

export default ThirdPartyPackagesSection;
