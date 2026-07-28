# Meta 9 - Co-op Permissions

`FarmPermissionPolicy` is the explicit local-mutation boundary. Solo and Host
roles may mutate shared management state only while simulation authority is
active. Peer roles never mutate locally; a future Steam adapter sends the
matching session intent and applies the returned host snapshot.

The first integrated management surface is build placement, movement, and
reclamation. The enum also reserves permissions for farm funds and session
management, ensuring the later UI does not hard-code role checks in unrelated
systems. Field work and commerce remain requestable gameplay actions; their
existing authoritative seams are unchanged.
