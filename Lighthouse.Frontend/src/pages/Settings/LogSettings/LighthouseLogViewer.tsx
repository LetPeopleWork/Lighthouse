import React from "react";

import { LazyLog } from "@melloware/react-logviewer";

interface LogViewerProps {
    data: string
}

const LighthouseLogViewer: React.FC<LogViewerProps> = ({ data }) => {
    return (        
        <LazyLog
            caseInsensitive
            enableHotKeys
            enableSearch
            height={'800'}
            extraLines={1}            
            selectableLines
            text={data} />
    )
}

export default LighthouseLogViewer;