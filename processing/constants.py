CANTONS = (
    "AG",
    "AI",
    "AR",
    "BE",
    "BL",
    "BS",
    "FR",
    "GE",
    "GL",
    "GR",
    "JU",
    "LU",
    "NE",
    "NW",
    "OW",
    "SG",
    "SH",
    "SO",
    "SZ",
    "TG",
    "TI",
    "UR",
    "VD",
    "VS",
    "ZG",
    "ZH",
)

PERIMETER_LN_SF = "lwb_perimeter_ln_sf"
REBBAUKATASTER = "lwb_rebbaukataster"
PERIMETER_TERRASSENREBEN = "lwb_perimeter_terrassenreben"
BIODIVERSITAETSFOERDERFLAECHEN = "lwb_biodiversitaetsfoerderflaechen"
BEWIRTSCHAFTUNGSEINHEIT = "lwb_bewirtschaftungseinheit"
NUTZUNGSFLAECHEN = "lwb_nutzungsflaechen"

V2_0 = "v2_0"

TOPICS = (
    {"base_topic": PERIMETER_LN_SF, "topic": PERIMETER_LN_SF + "_" + V2_0},
    {"base_topic": REBBAUKATASTER, "topic": REBBAUKATASTER + "_" + V2_0},
    {"base_topic": PERIMETER_TERRASSENREBEN, "topic": PERIMETER_TERRASSENREBEN + "_" + V2_0},
    {
        "base_topic": BIODIVERSITAETSFOERDERFLAECHEN,
        "topic": BIODIVERSITAETSFOERDERFLAECHEN + "_" + V2_0,
    },
    {"base_topic": BEWIRTSCHAFTUNGSEINHEIT, "topic": BEWIRTSCHAFTUNGSEINHEIT + "_" + V2_0},
    {"base_topic": NUTZUNGSFLAECHEN, "topic": NUTZUNGSFLAECHEN + "_" + V2_0},
)
