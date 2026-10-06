async function loadProjectStatus() {
  const status = document.querySelector("#project-status");
  const updated = document.querySelector("#updated-date");

  try {
    const response = await fetch("project-data.json", { cache: "no-store" });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const data = await response.json();
    status.textContent = data.status;
    updated.textContent = new Date(`${data.updated}T12:00:00`).toLocaleDateString(
      "en-US",
      { year: "numeric", month: "long", day: "numeric" }
    );
  } catch (error) {
    status.textContent = "Project scaffold prepared; live status unavailable.";
  }
}

loadProjectStatus();
