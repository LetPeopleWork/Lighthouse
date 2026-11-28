import { createContext } from "react";

/** The number of the page the user is on: 0 for the first page, one more after each move. */
export const PageMovesContext = createContext<number>(0);
