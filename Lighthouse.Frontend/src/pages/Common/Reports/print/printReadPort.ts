import type { IPortfolioService } from "../../../../services/Api/PortfolioService";
import type { IReportService } from "../../../../services/Api/ReportService";
import type { ITeamService } from "../../../../services/Api/TeamService";

/** What the print page may ask the server: the report and its owner's name, and nothing that changes anything. */
export type PrintReadPort = Pick<IReportService, "getReport"> &
	Pick<ITeamService, "getTeam"> &
	Pick<IPortfolioService, "getPortfolio">;
