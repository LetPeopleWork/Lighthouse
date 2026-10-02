use std::ffi::OsString;
#[cfg(target_os = "linux")]
use std::os::unix::ffi::{OsStrExt, OsStringExt};

/// The environment to start the backend with, derived from Tauri's own.
///
/// The AppImage launcher prepends its mount to LD_LIBRARY_PATH and friends. The image ships
/// a libcrypto without a matching libssl, so the backend would pair it with the host's newer
/// libssl and .NET aborts on startup. Inside an AppImage every entry under the mount is dropped.
#[cfg(target_os = "linux")]
pub fn backend_environment(
    parent: impl IntoIterator<Item = (OsString, OsString)>,
) -> Vec<(OsString, OsString)> {
    let parent: Vec<(OsString, OsString)> = parent.into_iter().collect();
    let appdir = parent
        .iter()
        .find(|(key, _)| key == "APPDIR")
        .map(|(_, value)| value.as_bytes().to_vec())
        .filter(|value| !value.is_empty());

    match appdir {
        None => parent,
        Some(appdir) => parent
            .into_iter()
            .filter(|(key, _)| !APPIMAGE_IDENTITY.iter().any(|identity| key == identity))
            .filter_map(|(key, value)| {
                without_appdir_entries(&value, &appdir).map(|value| (key, value))
            })
            .collect(),
    }
}

#[cfg(not(target_os = "linux"))]
pub fn backend_environment(
    parent: impl IntoIterator<Item = (OsString, OsString)>,
) -> Vec<(OsString, OsString)> {
    parent.into_iter().collect()
}

#[cfg(target_os = "linux")]
const APPIMAGE_IDENTITY: [&str; 5] = ["APPDIR", "APPIMAGE", "ARGV0", "OWD", "APPIMAGE_UUID"];

#[cfg(target_os = "linux")]
fn without_appdir_entries(value: &OsString, appdir: &[u8]) -> Option<OsString> {
    let entries: Vec<&[u8]> = value.as_bytes().split(|byte| *byte == b':').collect();
    if !entries.iter().any(|entry| is_under(entry, appdir)) {
        return Some(value.clone());
    }

    let kept: Vec<&[u8]> = entries
        .into_iter()
        .filter(|entry| !entry.is_empty() && !is_under(entry, appdir))
        .collect();

    if kept.is_empty() {
        None
    } else {
        Some(OsString::from_vec(kept.join(&b':')))
    }
}

#[cfg(target_os = "linux")]
fn is_under(entry: &[u8], appdir: &[u8]) -> bool {
    entry == appdir || (entry.starts_with(appdir) && entry.get(appdir.len()) == Some(&b'/'))
}

#[cfg(test)]
mod tests {
    use super::backend_environment;
    use std::ffi::OsString;

    fn env(pairs: &[(&str, &str)]) -> Vec<(OsString, OsString)> {
        pairs
            .iter()
            .map(|(key, value)| (OsString::from(key), OsString::from(value)))
            .collect()
    }

    #[cfg(target_os = "linux")]
    const APPDIR: &str = "/tmp/.mount_LighthAbc123";

    #[cfg(target_os = "linux")]
    fn inside_appimage(pairs: &[(&str, &str)]) -> Vec<(OsString, OsString)> {
        let mut parent = env(&[("APPDIR", APPDIR)]);
        parent.extend(pairs.iter().map(|(key, value)| {
            (
                OsString::from(key),
                OsString::from(value.replace("$APPDIR", APPDIR)),
            )
        }));
        backend_environment(parent)
    }

    #[cfg(target_os = "linux")]
    fn value_of<'a>(output: &'a [(OsString, OsString)], key: &str) -> Option<&'a OsString> {
        output
            .iter()
            .find(|(candidate, _)| candidate == key)
            .map(|(_, value)| value)
    }

    #[cfg(target_os = "linux")]
    #[test]
    fn strips_appimage_entries_from_ld_library_path() {
        let only_appimage =
            inside_appimage(&[("LD_LIBRARY_PATH", "$APPDIR/usr/lib/:$APPDIR/lib64/:")]);
        assert_eq!(value_of(&only_appimage, "LD_LIBRARY_PATH"), None);

        let mixed = inside_appimage(&[("LD_LIBRARY_PATH", "/opt/x:$APPDIR/usr/lib/:")]);
        assert_eq!(
            value_of(&mixed, "LD_LIBRARY_PATH"),
            Some(&OsString::from("/opt/x"))
        );
    }

    #[cfg(target_os = "linux")]
    #[test]
    fn restores_path_and_xdg_data_dirs_to_host_values() {
        let output = inside_appimage(&[
            ("PATH", "$APPDIR/usr/bin/:/usr/local/bin:/usr/bin"),
            (
                "XDG_DATA_DIRS",
                "$APPDIR/usr/share/:/usr/local/share:/usr/share",
            ),
        ]);

        assert_eq!(
            value_of(&output, "PATH"),
            Some(&OsString::from("/usr/local/bin:/usr/bin"))
        );
        assert_eq!(
            value_of(&output, "XDG_DATA_DIRS"),
            Some(&OsString::from("/usr/local/share:/usr/share"))
        );
    }

    #[cfg(target_os = "linux")]
    #[test]
    fn removes_appimage_identity_and_module_vars() {
        let output = inside_appimage(&[
            ("APPIMAGE", "/home/user/Lighthouse.AppImage"),
            ("ARGV0", "./Lighthouse.AppImage"),
            ("OWD", "/home/user"),
            ("APPIMAGE_UUID", "abc123"),
            ("GIO_MODULE_DIR", "$APPDIR//usr/lib/gio/modules"),
            ("GTK_PATH", "$APPDIR/usr/lib/gtk-3.0"),
            (
                "GDK_PIXBUF_MODULE_FILE",
                "$APPDIR/usr/lib/gdk-pixbuf-2.0/2.10.0/loaders.cache",
            ),
            ("GTK_DATA_PREFIX", "$APPDIR"),
        ]);

        for key in [
            "APPDIR",
            "APPIMAGE",
            "ARGV0",
            "OWD",
            "APPIMAGE_UUID",
            "GIO_MODULE_DIR",
            "GTK_PATH",
            "GDK_PIXBUF_MODULE_FILE",
            "GTK_DATA_PREFIX",
        ] {
            assert_eq!(value_of(&output, key), None, "{key} should be absent");
        }
    }

    #[cfg(target_os = "linux")]
    #[test]
    fn keeps_unrelated_user_configuration() {
        let unrelated = [
            ("HOME", "/home/user"),
            ("DOTNET_ROOT", "/usr/share/dotnet"),
            ("Lighthouse__Foo", "bar baz"),
            ("SSL_CERT_FILE", "/etc/ssl/cert.pem"),
            ("GTK_THEME", "Adwaita:dark"),
            ("MY_SEARCH_PATH", "/a:/b/c:/d"),
            ("ASPNETCORE_URLS", "http://[::1]:5000"),
            ("FOO", ":a::b:"),
            ("EMPTY", ""),
        ];

        let output = inside_appimage(&unrelated);

        for (key, value) in unrelated {
            assert_eq!(value_of(&output, key), Some(&OsString::from(value)));
        }
    }

    #[cfg(target_os = "linux")]
    #[test]
    fn keeps_non_utf8_bytes_in_kept_entries() {
        use std::os::unix::ffi::OsStringExt;

        let mut raw = b"/opt/caf\xe9:".to_vec();
        raw.extend_from_slice(APPDIR.as_bytes());
        raw.extend_from_slice(b"/usr/lib");
        let parent = vec![
            (OsString::from("APPDIR"), OsString::from(APPDIR)),
            (OsString::from("LD_LIBRARY_PATH"), OsString::from_vec(raw)),
        ];

        let output = backend_environment(parent);

        assert_eq!(
            output,
            vec![(
                OsString::from("LD_LIBRARY_PATH"),
                OsString::from_vec(b"/opt/caf\xe9".to_vec())
            )]
        );
    }

    #[test]
    fn passes_environment_through_unchanged_outside_appimage() {
        for appdir in [None, Some("")] {
            let mut parent = env(&[
                ("LD_LIBRARY_PATH", "/opt/x:/tmp/.mount_X/usr/lib/:"),
                ("PATH", "/usr/local/bin:/usr/bin"),
                ("HOME", "/home/user"),
                ("EMPTY", ""),
            ]);
            if let Some(value) = appdir {
                parent.insert(1, (OsString::from("APPDIR"), OsString::from(value)));
            }

            assert_eq!(backend_environment(parent.clone()), parent);
        }
    }
}
