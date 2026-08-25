import React from "react";
import ReactDOM from "react-dom/client";
import Root from "./club-page";
import "./styles.css";

const container = document.getElementById("root");
if (container === null) throw new Error("#root bulunamadı");

ReactDOM.createRoot(container).render(
  <React.StrictMode>
    <Root />
  </React.StrictMode>,
);
