import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Link from "@mui/material/Link";
import TextField from "@mui/material/TextField";
import { type GridValidRowModel, useGridApiContext } from "@mui/x-data-grid";
import type React from "react";
import { useContext, useEffect, useMemo, useRef, useState } from "react";
import DataGridBase from "../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import type { ThirdPartyPackage } from "../../../models/SystemInfo/ThirdPartyPackage";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { CycloneDxDocument } from "../../../services/Api/SystemInfoService";

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

/**
 * The search filters the packages before the grid sees them, so to the grid a search is only new
 * rows and it would keep the open page. A reader cannot tell a search from a column filter, so a
 * search opens the first page as a filter does, leaving the sort and any column filter in force.
 */
const OpensFirstPageOnSearch: React.FC<Readonly<{ searchText: string }>> = ({
	searchText,
}) => {
	const apiRef = useGridApiContext();
	const searchedFor = useRef(searchText);
	useEffect(() => {
		if (searchedFor.current === searchText) return;
		searchedFor.current = searchText;
		apiRef.current?.setPage(0);
	}, [apiRef, searchText]);
	return null;
};

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
				const backendPackages = parseCdxComponents(backendSbom);
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
		loadSbomData().catch((error) => {
			console.error("Error loading package information:", error);
		});
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
				initialSortModel={[{ field: "name", sort: "asc" }]}
				toolbarActions={<OpensFirstPageOnSearch searchText={searchText} />}
			/>
		</Box>
	);
};

export default ThirdPartyPackagesSection;
