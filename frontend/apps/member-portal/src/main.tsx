import React from "react";
import ReactDOM from "react-dom/client";
import { RouterProvider } from "react-router";
import { AppProviders } from "./app/providers";
import { createRouter } from "./app/router";
import { Toaster } from "./shared/toaster";
import "./styles.css";

const container = document.getElementById("root");
if (container === null) throw new Error("#root bulunamadı");

ReactDOM.createRoot(container).render(
  <React.StrictMode>
    <AppProviders>
      <RouterProvider router={createRouter()} />
      <Toaster />
    </AppProviders>
  </React.StrictMode>,
);
