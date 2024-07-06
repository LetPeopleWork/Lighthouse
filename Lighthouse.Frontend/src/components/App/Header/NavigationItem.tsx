import React from "react";
import { NavLink } from "react-router-dom";

interface NavigationItemProps{
    path: string;
    text: string;
}


const NavigationItem : React.FC<NavigationItemProps> = ({path, text}) => {
    return (
        <NavLink to={path} className={({isActive}) => (isActive ? 'nav-link active': 'nav-link')}>{text}</NavLink>
    )
}

export default NavigationItem;