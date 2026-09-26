// Skyline Rush branding modification. Original engine copyright retained in doc/license.txt.
#define STR_MACRO_HELPER(s) #s
#define STR_MACRO(s) STR_MACRO_HELPER(s)

#ifdef __clang__
  #define VERSION_COMP "clang-" \
    STR_MACRO(__clang_major__) "." \
    STR_MACRO(__clang_minor__) "." \
    STR_MACRO(__clang_patchlevel__)
#elif defined(_MSC_VER)
  #define VERSION_COMP "msvc-" STR_MACRO(_MSC_VER)
#else
  #define VERSION_COMP "gnuc-" \
    STR_MACRO(__GNUC__) "." \
    STR_MACRO(__GNUC_MINOR__) "." \
    STR_MACRO(__GNUC_PATCHLEVEL__)
#endif

#define VERSION_MAJOR 2
#define VERSION_MINOR 0
#define VERSION_PATCH 9
#define VERSION_HLP(x,y,z,r) #x#r#y#r#z
#define VERSION_STR(x,y,z,r) VERSION_HLP(x,y,z,r)
#define VERSION_STRING VERSION_STR(VERSION_MAJOR,VERSION_MINOR,VERSION_PATCH,.)
#define VERSION_NAME "Skyline Rush"
#define VERSION_FNAME "Skyline Rush — Playtest"
#define VERSION_UNAME "skyline-rush"
#define VERSION_VNAME "SKYLINERUSH"
#define VERSION_RELEASE "District Playtest 0.2"
#define VERSION_URL "github.com/jakeharvey162-source"
#define VERSION_COPY "2009-2025"
#define VERSION_DESC "An independent rooftop arena prototype based on Red Eclipse"
#define VERSION_STEAM_APPID 967460
#define VERSION_STEAM_DEPOT 967461
#define VERSION_DISCORD "506825464946360321"

#ifndef VERSION_BUILD
#define VERSION_BUILD 0
#endif
#ifndef VERSION_BRANCH
#define VERSION_BRANCH "selfbuilt"
#endif
#ifndef VERSION_REVISION
#define VERSION_REVISION ""
#endif

#define LAN_PORT 29799
#define MASTER_PORT 29800
#define SERVER_PORT 29801
